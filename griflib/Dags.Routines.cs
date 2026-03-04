using System.Text;
using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    public static ScriptObj CreateScript(string? script)
    {
        return new ScriptObj
        {
            Tokens = SplitTokens(script),
            Index = 0
        };
    }

    /// <summary>
    /// Split the script into script.Tokens for processing.
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
    public static string PrettyScript(string? scriptText, bool indent = false)
    {
        StringBuilder result = new();

        if (!IsScript(scriptText))
        {
            if (indent)
            {
                result.Append('\t');
            }
            result.Append(scriptText);
            return result.ToString();
        }

        int startIndent = indent ? 1 : 0;
        int indentLevel = startIndent;
        int parens = 0;
        bool ifLine = false;
        bool forLine = false;
        bool forEachKeyLine = false;
        bool forEachListLine = false;
        var script = CreateScript(scriptText);

        foreach (string s in script.Tokens)
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
    public static string CompressScript(string? scriptText)
    {
        if (!IsScript(scriptText))
        {
            return scriptText ?? "";
        }
        StringBuilder result = new();
        var script = CreateScript(scriptText);
        char lastChar = ',';
        bool addSpace;
        foreach (string s in script.Tokens)
        {
            addSpace = false;
            if (s.StartsWith(SCRIPT_CHAR))
            {
                if (lastChar != '(' && lastChar != ',')
                {
                    addSpace = true;
                }
            }
            else if (lastChar == '(')
            {
                addSpace = false;
            }
            else if (s == ")" || s == ",")
            {
                addSpace = false;
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
    public static bool ValidateScript(string? scriptText)
    {
        if (!IsScript(scriptText))
        {
            return true;
        }
        var script = CreateScript(scriptText);
        int parens = 0;
        int ifCount = 0;
        bool inIf = false;
        int forCount = 0;
        int forEachKeyCount = 0;
        int forEachListCount = 0;
        foreach (string s in script.Tokens)
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

    /// <summary>
    /// Determines if the key is valid. Function keys start with @ and can have parameters.
    /// Non-function keys can have periods.
    /// </summary>
    public static bool ValidKey(string key, bool functionKey)
    {
        if (string.IsNullOrEmpty(key)) return false;
        bool inParen = false;
        bool lastComma = false;
        for (int index = 0; index < key.Length; index++)
        {
            char c = key[index];
            if (index == 0 && functionKey && c != '@')
            {
                return false;
            }
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                lastComma = false;
                continue;
            }
            if (!functionKey)
            {
                if (c == '.')
                {
                    lastComma = false;
                    continue;
                }
                return false;
            }
            else
            {
                if (c == '@')
                {
                    if (index > 0) return false;
                    lastComma = false;
                    continue;
                }
                if (c == '(')
                {
                    if (inParen) return false;
                    inParen = true;
                    lastComma = true; // so no initial comma
                    continue;
                }
                if (inParen)
                {
                    if (c == ',')
                    {
                        if (lastComma) return false;
                        lastComma = true;
                        continue;
                    }
                    if (c == ')')
                    {
                        if (lastComma) return false; // so no trailing comma, no empty parens
                        if (index < key.Length - 1) return false;
                        inParen = false;
                        continue;
                    }
                }
                return false;
            }
        }
        if (inParen) return false;
        return true;
    }

    #region Private routines

    /// <summary>
    /// Get parameters from script.Tokens starting at the specified script.Index.
    /// </summary>
    private static List<GrifMessage> GetParameters(Grod grod, ScriptObj script)
    {
        List<GrifMessage> parameters = [];
        while (script.Index < script.Tokens.Length && script.Tokens[script.Index] != ")")
        {
            var token = script.Tokens[script.Index];
            if (IsScript(token))
            {
                // Handle nested script.Tokens
                parameters.AddRange(ProcessOneCommand(grod, script));
            }
            else
            {
                parameters.Add(new GrifMessage(MessageType.Internal, TrimQuotes(token)));
                script.Index++;
            }
            if (parameters.Any(x => x.Type == MessageType.Error))
            {
                script.Index = script.Tokens.Length; // Stop processing on error
                return [.. parameters.Where(x => x.Type == MessageType.Error)];
            }
            if (script.Index < script.Tokens.Length)
            {
                if (script.Tokens[script.Index] == ")")
                {
                    break; // End of parameters
                }
                if (script.Tokens[script.Index] != ",")
                {
                    throw new SystemException("Missing comma in parameters");
                }
                script.Index++; // Skip the comma
            }
        }
        if (script.Index >= script.Tokens.Length || script.Tokens[script.Index] != ")")
        {
            throw new SystemException("Missing closing parenthesis");
        }
        script.Index++; // Skip the closing parenthesis
        return parameters;
    }

    /// <summary>
    /// Trim surrounding quotes from a string value.
    /// </summary>
    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            value = value[1..^1]; // Remove surrounding quotes
            value = value.Replace("\\\"", "\"");
        }
        return value;
    }

    /// <summary>
    /// Check that the parameter count matches the expected count.
    /// </summary>
    private static void CheckParameterCount(List<GrifMessage> p, long count)
    {
        if (p.Count != count)
        {
            throw new ArgumentException($"Expected {count} parameters, but got {p.Count}");
        }
    }

    /// <summary>
    /// Check that there is at least one parameter.
    /// </summary>
    private static void CheckParameterAtLeastOne(List<GrifMessage> p)
    {
        if (p.Count == 0)
        {
            throw new ArgumentException($"Expected at least one parameter, but got {p.Count}");
        }
    }

    /// <summary>
    /// Check that the parameter count is between min and max.
    /// </summary>
    private static void CheckParmeterCountBetween(List<GrifMessage> p, long min, long max)
    {
        if (p.Count < min || p.Count > max)
        {
            throw new ArgumentException($"Expected between {min} and {max} parameters, but got {p.Count}");
        }
    }

    /// <summary>
    /// Get the string value from a Grod, processing scripts as needed.
    /// </summary>
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

    /// <summary>
    /// Get user-defined function values.
    /// </summary>
    private static List<GrifMessage> GetUserDefinedFunctionValues(Grod grod, ScriptObj script, string token, List<GrifMessage> p)
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
        var value = GetGlobalOrLocal(grod, script, key, true)
            ?? throw new SystemException($"Key not found: {keys.First()}");
        for (int i = 0; i < placeholders.Length; i++)
        {
            var placeholder = placeholders[i].Trim();
            value = value.Replace("$" + placeholder, p[i].Value, OIC);
        }
        var userResult = Process(grod, value);
        return userResult;
    }

    /// <summary>
    /// Return "true" or "false" string based on the boolean value.
    /// </summary>
    private static string TrueFalse(bool value)
    {
        return value ? TRUE : FALSE;
    }

    /// <summary>
    /// Evaluate a string as a boolean value.
    /// </summary>
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

    /// <summary>
    /// Add an item to a comma-delimited list in the Grod.
    /// </summary>
    private static void AddListItem(Grod grod, ScriptObj script, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        value = FixListItemIn(value);
        var existing = GetGlobalOrLocal(grod, script, key, true);
        if (string.IsNullOrEmpty(existing) || IsNull(existing))
        {
            SetGlobalOrLocal(grod, script, key, value);
        }
        else
        {
            SetGlobalOrLocal(grod, script, key, existing + "," + value);
        }
    }

    /// <summary>
    /// Clear all items in an array stored in the Grod.
    /// </summary>
    private static void ClearArray(Grod grod, ScriptObj script, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        var list = grod.Keys(key + ":", true, true);
        foreach (var item in list)
        {
            SetGlobalOrLocal(grod, script, item, null);
        }
    }

    /// <summary>
    /// Fix a list item for storage by replacing commas and handling nulls.
    /// </summary>
    private static string FixListItemIn(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return NULL;
        if (value.Contains(','))
            value = value.Replace(",", COMMA_CHAR);
        return value;
    }

    /// <summary>
    /// Fix a list item for output by restoring commas and handling nulls.
    /// </summary>
    private static string? FixListItemOut(string value)
    {
        if (value == NULL)
            return null;
        if (value.Contains(COMMA_CHAR))
            value = value.Replace(COMMA_CHAR, ",");
        return value;
    }

    /// <summary>
    /// Get an item from a 2D array stored in the Grod.
    /// </summary>
    private static string? GetArrayItem(Grod grod, ScriptObj script, string key, long y, long x)
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
        return GetListItem(grod, script, itemKey, x);
    }

    /// <summary>
    /// Set an item in a 2D array stored in the Grod.
    /// </summary>
    private static void SetArrayItem(Grod grod, ScriptObj script, string key, long y, long x, string? value)
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
        SetListItem(grod, script, itemKey, x, value);
    }

    /// <summary>
    /// Get an item from a comma-delimited list in the Grod.
    /// </summary>
    private static string? GetListItem(Grod grod, ScriptObj script, string key, long x)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = GetGlobalOrLocal(grod, script, key, true);
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

    /// <summary>
    /// Set an item in a comma-delimited list in the Grod.
    /// </summary>
    private static void SetListItem(Grod grod, ScriptObj script, string key, long x, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = GetGlobalOrLocal(grod, script, key, true);
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
        SetGlobalOrLocal(grod, script, key, string.Join(',', items));
    }

    private static bool InList(Grod grod, ScriptObj script, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (value == null || value.Equals(NULL, OIC))
        {
            return false;
        }
        var list = GetGlobalOrLocal(grod, script, key, true);
        if (string.IsNullOrWhiteSpace(list) || IsNull(list))
        {
            return false;
        }
        var items = SplitList(list).ToList();
        foreach (var item in items)
        {
            if (item.Equals(value, OIC))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Insert an item at a specific script.Index in a comma-delimited list in the Grod.
    /// </summary>
    private static void InsertAtListItem(Grod grod, ScriptObj script, string key, long x, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = GetGlobalOrLocal(grod, script, key, true);
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
        SetGlobalOrLocal(grod, script, key, string.Join(',', items));
    }

    /// <summary>
    /// Remove an item at a specific script.Index in a comma-delimited list in the Grod.
    /// </summary>
    private static void RemoveAtListItem(Grod grod, ScriptObj script, string key, long x)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new SystemException("Key cannot be null or empty.");
        }
        if (x < 0)
        {
            throw new SystemException($"List indexes cannot be negative: {key}: {x}");
        }
        var list = GetGlobalOrLocal(grod, script, key, true);
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
        SetGlobalOrLocal(grod, script, key, string.Join(',', items));
    }

    /// <summary>
    /// Get a value from either the local script Grod or the global Grod.
    /// </summary>
    private static string? GetGlobalOrLocal(Grod grod, ScriptObj script, string key, bool recursive)
    {
        if (IsLocal(key))
        {
            return script.LocalData.Get(key, recursive);
        }
        else
        {
            return grod.Get(key, recursive);
        }
    }

    private static void SetGlobalOrLocal(Grod grod, ScriptObj script, string key, string? value)
    {
        if (IsLocal(key))
        {
            script.LocalData.Set(key, value);
        }
        else
        {
            grod.Set(key, value);
        }
    }

    /// <summary>
    /// Get whether the value is a local variable.
    /// </summary>
    private static bool IsLocal(string? value)
    {
        return value != null && value.StartsWith(LOCAL_CHAR);
    }

    /// <summary>
    /// Get the condition for an if or while statement.
    /// </summary>
    private static bool GetCondition(Grod grod, ScriptObj script)
    {
        var answer = ProcessOneCommand(grod, script);
        if (script.ReturnFlag)
        {
            if (answer.Count == 1 && answer[0].Type == MessageType.Error)
            {
                throw new SystemException(answer[0].Value);
            }
            if (answer.Count == 1)
            {
                return IsTrue(answer[0].Value);
            }
            return false;
        }
        if (answer.Count != 1 ||
            (answer[0].Type != MessageType.Text && answer[0].Type != MessageType.Internal))
        {
            throw new SystemException("Invalid condition");
        }
        return IsTrue(answer[0].Value);
    }

    #endregion
}
