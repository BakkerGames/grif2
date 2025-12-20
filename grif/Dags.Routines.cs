using System.Text;
using static Grif.Common;

namespace Grif;

public partial class Dags
{
    /// <summary>
    /// Split the script into tokens for processing.
    /// </summary>
    public static string[] SplitTokens(string? script)
    {
        List<string> result = [];
        if (string.IsNullOrWhiteSpace(script))
        {
            return [.. result];
        }
        StringBuilder token = new();
        bool inToken = false;
        bool inQuote = false;
        bool inAtFunction = false;
        bool lastSlash = false;
        foreach (char c in script)
        {
            if (inQuote)
            {
                if (c == '"' && !lastSlash)
                {
                    token.Append(c);
                    if (inAtFunction)
                    {
                        result.Add(token.ToString());
                        inAtFunction = false;
                    }
                    else
                    {
                        result.Add(token.ToString());
                    }
                    token.Clear();
                    inQuote = false;
                    inToken = false;
                    continue;
                }
                if (c == '"' && lastSlash)
                {
                    token.Append("\\\"");
                    lastSlash = false;
                    continue;
                }
                if (c == '\\' && !lastSlash)
                {
                    lastSlash = true;
                    continue;
                }
                if (lastSlash)
                {
                    token.Append('\\');
                    lastSlash = false;
                }
                token.Append(c);
                continue;
            }
            if (c == ',' || c == ')' || c == '[' || c == ']')
            {
                if (token.Length > 0)
                {
                    if (inAtFunction)
                    {
                        result.Add(token.ToString());
                        inAtFunction = false;
                    }
                    else
                    {
                        result.Add(token.ToString());
                    }
                    token.Clear();
                }
                result.Add(c.ToString());
                inToken = false;
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                if (!inToken)
                {
                    continue;
                }
                result.Add(token.ToString());
                token.Clear();
                inToken = false;
                continue;
            }
            if (!inToken)
            {
                if (c == '"')
                {
                    inQuote = true;
                    token.Append(c);
                }
                else
                {
                    inAtFunction = (c == '@');
                    token.Append(c);
                }
                inToken = true;
                continue;
            }
            if (c == '@')
            {
                result.Add(token.ToString());
                token.Clear();
                token.Append(c);
                continue;
            }
            if (c == '(')
            {
                if (token.Length > 0)
                {
                    token.Append(c);
                    if (inAtFunction)
                    {
                        result.Add(token.ToString());
                        inAtFunction = false;
                    }
                    else
                    {
                        result.Add(token.ToString());
                    }
                    token.Clear();
                }
                inToken = false;
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                if (token.Length > 0)
                {
                    if (inAtFunction)
                    {
                        result.Add(token.ToString());
                        inAtFunction = false;
                    }
                    else
                    {
                        result.Add(token.ToString());
                    }
                    token.Clear();
                }
                inToken = false;
                continue;
            }
            token.Append(c);
        }
        if (token.Length > 0)
        {
            result.Add(token.ToString());
            token.Clear();
        }
        return [.. result];
    }

    /// <summary>
    /// Format the script with line breaks and indents.
    /// Parameter "indent" adds one extra tab at the beginning of each line.
    /// </summary>
    public static string PrettyScript(string? script, bool indent = false)
    {
        StringBuilder result = new();

        if (!IsScript(script))
        {
            if (indent)
            {
                result.Append('\t');
            }
            result.Append(script);
            return result.ToString();
        }

        int startIndent = indent ? 1 : 0;
        int indentLevel = startIndent;
        int parens = 0;
        bool ifLine = false;
        bool forLine = false;
        bool forEachKeyLine = false;
        bool forEachListLine = false;
        var tokens = SplitTokens(script);

        foreach (string s in tokens)
        {
            switch (s.ToLower())
            {
                case ELSEIF_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
                case ELSE_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
                case ENDIF_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
                case ENDFOR_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
                case ENDFOREACHKEY_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
                case ENDFOREACHLIST_TOKEN:
                    if (indentLevel > startIndent) indentLevel--;
                    break;
            }
            if (parens == 0)
            {
                if (ifLine)
                {
                    result.Append(' ');
                }
                else
                {
                    if (result.Length > 0)
                    {
                        result.AppendLine();
                    }
                    if (indentLevel > 0)
                    {
                        result.Append(new string('\t', indentLevel));
                    }
                }
            }
            result.Append(s);
            switch (s.ToLower())
            {
                case IF_TOKEN:
                    ifLine = true;
                    break;
                case ELSEIF_TOKEN:
                    ifLine = true;
                    break;
                case ELSE_TOKEN:
                    indentLevel++;
                    break;
                case THEN_TOKEN:
                    indentLevel++;
                    ifLine = false;
                    break;
                case FOR_TOKEN:
                    forLine = true;
                    break;
                case FOREACHKEY_TOKEN:
                    forEachKeyLine = true;
                    break;
                case FOREACHLIST_TOKEN:
                    forEachListLine = true;
                    break;
            }
            if (s.EndsWith('('))
            {
                parens++;
            }
            else if (s == ")")
            {
                if (parens > 0) parens--;
                if (forLine && parens == 0)
                {
                    forLine = false;
                    indentLevel++;
                }
                else if (forEachKeyLine && parens == 0)
                {
                    forEachKeyLine = false;
                    indentLevel++;
                }
                else if (forEachListLine && parens == 0)
                {
                    forEachListLine = false;
                    indentLevel++;
                }
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Format the script in a single line with minimal spaces.
    /// </summary>
    public static string CompressScript(string? script)
    {
        if (!IsScript(script))
        {
            return script ?? "";
        }
        StringBuilder result = new();
        var tokens = SplitTokens(script);
        char lastChar = ',';
        bool addSpace;
        foreach (string s in tokens)
        {
            addSpace = false;
            if (s.StartsWith(SCRIPT_CHAR))
            {
                if (lastChar != '(' && lastChar != ',')
                {
                    addSpace = true;
                }
            }
            else
            {
                addSpace = true;
            }
            if (addSpace)
            {
                result.Append(' ');
            }
            result.Append(s);
            lastChar = s[^1];
        }
        return result.ToString();
    }

    /// <summary>
    /// Validate the script for correct syntax.
    /// </summary>
    public static bool ValidateScript(string? script)
    {
        if (!IsScript(script))
        {
            return true;
        }
        var tokens = SplitTokens(script);
        int parens = 0;
        int ifCount = 0;
        bool inIf = false;
        int forCount = 0;
        int forEachKeyCount = 0;
        int forEachListCount = 0;
        foreach (string s in tokens)
        {
            if (s.StartsWith(SCRIPT_CHAR) && s.EndsWith('('))
            {
                parens++;
            }
            else if (s == ")")
            {
                parens--;
                if (parens < 0)
                {
                    return false; // More closing parens than opening
                }
            }
            switch (s.ToLower())
            {
                case IF_TOKEN:
                    ifCount++;
                    inIf = true;
                    break;
                case AND_TOKEN:
                case OR_TOKEN:
                case NOT_TOKEN:
                    if (!inIf)
                    {
                        return false; // Logical operator outside of @if
                    }
                    break;
                case THEN_TOKEN:
                case ELSE_TOKEN:
                    inIf = false;
                    if (ifCount == 0)
                    {
                        return false; // @then, @else without matching @if
                    }
                    break;
                case ELSEIF_TOKEN:
                    inIf = true;
                    if (ifCount == 0)
                    {
                        return false; // @elseif without matching @if
                    }
                    break;
                case ENDIF_TOKEN:
                    ifCount--;
                    inIf = false;
                    if (ifCount < 0)
                    {
                        return false; // More @endif than @if
                    }
                    break;
                case FOR_TOKEN:
                    forCount++;
                    break;
                case FOREACHKEY_TOKEN:
                    forEachKeyCount++;
                    break;
                case FOREACHLIST_TOKEN:
                    forEachListCount++;
                    break;
                case ENDFOR_TOKEN:
                    forCount--;
                    if (forCount < 0)
                    {
                        return false; // More @endfor than @for
                    }
                    break;
                case ENDFOREACHKEY_TOKEN:
                    forEachKeyCount--;
                    if (forEachKeyCount < 0)
                    {
                        return false; // More @endforeachkey than @foreachkey
                    }
                    break;
                case ENDFOREACHLIST_TOKEN:
                    forEachListCount--;
                    if (forEachListCount < 0)
                    {
                        return false; // More @endforeachlist than @foreachlist
                    }
                    break;
            }
        }
        if (parens != 0)
        {
            return false; // parens not balanced
        }
        if (ifCount != 0 || inIf)
        {
            return false; // if not balanced
        }
        if (forCount != 0)
        {
            return false; // for loops not balanced
        }
        if (forEachKeyCount != 0)
        {
            return false; // foreachkey loops not balanced
        }
        if (forEachListCount != 0)
        {
            return false; // foreachlist loops not balanced
        }
        return true;
    }

    /// <summary>
    /// Get long value from string, with error handling.
    /// </summary>
    public static long GetNumberValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0; // Default to 0 if not found
        }
        if (!long.TryParse(value, out long longValue))
        {
            throw new SystemException($"Invalid number: {value}");
        }
        return longValue;
    }

    /// <summary>
    /// Determines if the value is a script.
    /// </summary>
    public static bool IsScript(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        int pos = 0;
        SkipWhitespace(value, ref pos);
        return pos < value.Length && value[pos] == SCRIPT_CHAR;
    }

    /// <summary>
    /// Determines if the value is null or the special NULL string.
    /// </summary>
    public static bool IsNull(string? value)
    {
        return value == null || value.Equals(NULL, OIC);
    }

    /// <summary>
    /// Determines whether the specified string is null, empty, or the special NULL string.
    /// </summary>
    public static bool IsNullOrEmpty(string? value)
    {
        return value == null || value.Equals(NULL, OIC) || value == "";
    }

    /// <summary>
    /// Splits a comma-delimited string into an array of items, normalizing each element.
    /// </summary>
    public static string[] SplitList(string? list)
    {
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            return [];
        }
        var items = list.Split(',');
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = FixListItemOut(items[i]) ?? "";
        }
        return items;
    }

    #region Private routines

    private static List<GrifMessage> GetParameters(string[] tokens, ref int index, Grod grod)
    {
        List<GrifMessage> parameters = [];
        while (index < tokens.Length && tokens[index] != ")")
        {
            var token = tokens[index];
            if (IsScript(token))
            {
                // Handle nested tokens
                parameters.AddRange(ProcessOneCommand(tokens, ref index, grod));
            }
            else
            {
                parameters.Add(new GrifMessage(MessageType.Internal, TrimQuotes(token)));
                index++;
            }
            if (index < tokens.Length)
            {
                if (tokens[index] == ")")
                {
                    break; // End of parameters
                }
                if (tokens[index] != ",")
                {
                    throw new SystemException("Missing comma in parameters");
                }
                index++; // Skip the comma
            }
        }
        if (index >= tokens.Length || tokens[index] != ")")
        {
            throw new SystemException("Missing closing parenthesis");
        }
        index++; // Skip the closing parenthesis
        return parameters;
    }

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            value = value[1..^1]; // Remove surrounding quotes
            value = value.Replace("\\\"", "\"");
        }
        return value;
    }

    private static void CheckParameterCount(List<GrifMessage> p, long count)
    {
        if (p.Count != count)
        {
            throw new ArgumentException($"Expected {count} parameters, but got {p.Count}");
        }
    }

    private static void CheckParameterAtLeastOne(List<GrifMessage> p)
    {
        if (p.Count == 0)
        {
            throw new ArgumentException($"Expected at least one parameter, but got {p.Count}");
        }
    }

    private static void CheckParmeterCountBetween(List<GrifMessage> p, long min, long max)
    {
        if (p.Count < min || p.Count > max)
        {
            throw new ArgumentException($"Expected between {min} and {max} parameters, but got {p.Count}");
        }
    }

    private static string GetValue(Grod grod, string? value)
    {
        if (IsNull(value))
        {
            return "";
        }
        if (!IsScript(value))
        {
            return value ?? "";
        }
        var resultItems = Process(grod, value);
        if (resultItems.Count == 1 &&
            (resultItems[0].Type == MessageType.Text || resultItems[0].Type == MessageType.Internal))
        {
            return GetValue(grod, resultItems[0].Value);
        }
        throw new SystemException("Expected a single text result.");
    }

    private static List<GrifMessage> GetUserDefinedFunctionValues(string token, List<GrifMessage> p, Grod grod)
    {
        var keys = grod.Keys(token, true, true);
        if (keys.Count == 0)
        {
            throw new SystemException($"Unknown token: {token}");
        }
        if (keys.Count > 1)
        {
            throw new SystemException($"Multiple definitions found for {token}");
        }
        var key = keys.First();
        if (!key.EndsWith(')'))
        {
            throw new SystemException($"Invalid key format: {key}");
        }
        var placeholders = key[(key.IndexOf('(') + 1)..^1].Split(',');
        if (placeholders.Length != p.Count)
        {
            throw new SystemException($"Parameter count mismatch for {key}. Expected {placeholders.Length}, got {p.Count}");
        }
        var value = grod.Get(key, true)
            ?? throw new SystemException($"Key not found: {keys.First()}");
        for (int i = 0; i < placeholders.Length; i++)
        {
            var placeholder = placeholders[i].Trim();
            value = value.Replace("$" + placeholder, p[i].Value, OIC);
        }
        var userResult = Process(grod, value);
        return userResult;
    }

    private static string TrueFalse(bool value)
    {
        return value ? TRUE : FALSE;
    }

    private static bool IsTrue(string? value)
    {
        if (value == null)
        {
            return false; // Treat null as false
        }
        return value.ToLower() switch
        {
            TRUE or "t" or "yes" or "y" or "1" or "-1" => true,
            FALSE or "f" or "no" or "n" or "0" or NULL or "" => false,
            _ => throw new SystemException($"Non-boolean value: {value}"),
        };
    }

    private static void AddListItem(Grod grod, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        value = FixListItemIn(value);
        var existing = grod.Get(key, true);
        if (string.IsNullOrEmpty(existing) || IsNull(existing))
        {
            grod.Set(key, value);
        }
        else
        {
            grod.Set(key, existing + "," + value);
        }
    }

    private static void ClearArray(Grod grod, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        var list = grod.Keys(key + ":", true, true);
        foreach (var item in list)
        {
            grod.Set(item, null);
        }
    }

    private static string FixListItemIn(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return NULL;
        if (value.Contains(','))
            value = value.Replace(",", COMMA_CHAR);
        return value;
    }

    private static string? FixListItemOut(string value)
    {
        if (value == NULL)
            return null;
        if (value.Contains(COMMA_CHAR))
            value = value.Replace(COMMA_CHAR, ",");
        return value;
    }

    private static string? GetArrayItem(Grod grod, string key, long y, long x)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (y < 0 || x < 0)
        {
            throw new SystemException($"Array indexes cannot be negative: {key}: {y},{x}");
        }
        var itemKey = $"{key}:{y}";
        return GetListItem(grod, itemKey, x);
    }

    private static void SetArrayItem(Grod grod, string key, long y, long x, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (y < 0 || x < 0)
        {
            throw new SystemException($"Array indexes cannot be negative: {key}: {y},{x}");
        }
        var itemKey = $"{key}:{y}";
        SetListItem(grod, itemKey, x, value);
    }

    private static string? GetListItem(Grod grod, string key, long x)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = grod.Get(key, true);
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            return null;
        }
        var items = SplitList(list);
        if (x >= items.Length)
        {
            return null;
        }
        return items[x];
    }

    private static void SetListItem(Grod grod, string key, long x, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = grod.Get(key, true);
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            list = NULL;
        }
        var items = SplitList(list).ToList();
        while (x >= items.Count)
        {
            items.Add(NULL);
        }
        items[(int)x] = FixListItemIn(value);
        grod.Set(key, string.Join(',', items));
    }

    private static void InsertAtListItem(Grod grod, string key, long x, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = grod.Get(key, true);
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            list = NULL;
        }
        var items = SplitList(list).ToList();
        while (x >= items.Count)
        {
            items.Add(NULL);
        }
        items.Insert((int)x, FixListItemIn(value));
        grod.Set(key, string.Join(',', items));
    }

    private static void RemoveAtListItem(Grod grod, string key, long x)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = grod.Get(key, true);
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            list = NULL;
        }
        var items = SplitList(list).ToList();
        while (x >= items.Count)
        {
            return; // Nothing to remove
        }
        items.RemoveAt((int)x);
        grod.Set(key, string.Join(',', items));
    }

    #endregion
}
