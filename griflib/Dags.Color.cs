using System.Text;
using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    /// <summary>
    /// Format the script with line breaks and indents. Colorize the script tokens and return a list of TextColorItem.
    /// </summary>
    public static List<TextColorItem> ColorizeScript(string script)
    {
        if (!script.TrimStart().StartsWith(SCRIPT_CHAR))
        {
            return [new TextColorItem(script, TextColorEnum.Default)];
        }
        ScriptObj scriptObj = CreateScript(script);
        return ColorizeScript(scriptObj);
    }

    /// <summary>
    /// Format the script with line breaks and indents. Colorize the script tokens and return a list of TextColorItem.
    /// </summary>
    public static List<TextColorItem> ColorizeScript(ScriptObj script)
    {
        List<TextColorItem> listResult = [];

        StringBuilder result = new();

        int startIndent = 0;
        int indentLevel = startIndent;
        int parens = 0;
        bool ifLine = false;
        bool forLine = false;
        bool forEachKeyLine = false;
        bool forEachListLine = false;

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
                    if (listResult.Count > 0)
                    {
                        result.Append('\n');
                    }
                    if (indentLevel > 0)
                    {
                        result.Append(new string(' ', indentLevel * 4));
                    }
                }
            }
            if (result.Length > 0)
            {
                listResult.Add(new TextColorItem(result.ToString(), TextColorEnum.Default));
                result.Clear();
            }
            listResult.AddRange(ColorizeToken(s));
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
        return listResult;
    }

    /// <summary>
    /// Colorize the script tokens.
    /// </summary>
    private static List<TextColorItem> ColorizeToken(string s)
    {
        List<TextColorItem> result = [];
        if (string.IsNullOrEmpty(s))
        {
            return result;
        }
        if (s.StartsWith(COMMENT_TOKEN, OIC) || s.StartsWith("//"))
        {
            result.Add(new TextColorItem(s[..^1], TextColorEnum.CommentColor));
            result.Add(new TextColorItem("(", TextColorEnum.ParenthesisColor));
        }
        else if (s.Equals(IF_TOKEN, OIC) ||
            s.Equals(THEN_TOKEN, OIC) ||
            s.Equals(AND_TOKEN, OIC) ||
            s.Equals(OR_TOKEN, OIC) ||
            s.Equals(NOT_TOKEN, OIC) ||
            s.Equals(ELSE_TOKEN, OIC) ||
            s.Equals(ELSEIF_TOKEN, OIC) ||
            s.Equals(ENDIF_TOKEN, OIC))
        {
            result.Add(new TextColorItem(s, TextColorEnum.IfColor));
        }
        else if (s.Equals(FOR_TOKEN, OIC) ||
            s.Equals(FOREACHKEY_TOKEN, OIC) ||
            s.Equals(FOREACHLIST_TOKEN, OIC) ||
            s.Equals(ENDFOR_TOKEN, OIC) ||
            s.Equals(ENDFOREACHKEY_TOKEN, OIC) ||
            s.Equals(ENDFOREACHLIST_TOKEN, OIC) ||
            s.Equals(WHILE_TOKEN, OIC) ||
            s.Equals(ENDWHILE_TOKEN, OIC))
        {
            if (s.EndsWith('('))
            {
                result.Add(new TextColorItem(s[..^1], TextColorEnum.ForColor));
                result.Add(new TextColorItem("(", TextColorEnum.ParenthesisColor));
            }
            else
            {
                result.Add(new TextColorItem(s, TextColorEnum.ForColor));
            }
        }
        else if (s.StartsWith(SCRIPT_CHAR))
        {
            if (s.EndsWith('('))
            {
                result.Add(new TextColorItem(s[..^1], TextColorEnum.TokenColor));
                result.Add(new TextColorItem("(", TextColorEnum.ParenthesisColor));
            }
            else
            {
                result.Add(new TextColorItem(s, TextColorEnum.TokenColor));
            }
        }
        else if (s == "," || s == "[" || s == "]")
        {
            result.Add(new TextColorItem(s, TextColorEnum.PunctuationColor));
        }
        else if (s == "(" || s == ")")
        {
            result.Add(new TextColorItem(s, TextColorEnum.ParenthesisColor));
        }
        else if (s.StartsWith('"') && s.EndsWith('"'))
        {
            result.Add(new TextColorItem(s, TextColorEnum.QuoteColor));
        }
        else if (s.StartsWith(LOCAL_CHAR) || s.StartsWith(PARAM_CHAR))
        {
            result.Add(new TextColorItem(s, TextColorEnum.ParameterColor));
        }
        else if (s.Contains(PARAM_CHAR))
        {
            while (s.Contains(PARAM_CHAR))
            {
                int pos1 = s.IndexOf(PARAM_CHAR);
                if (pos1 > 0)
                {
                    result.Add(new TextColorItem(s[..pos1], TextColorEnum.Default));
                    s = s[pos1..];
                }
                pos1 = s.Length;
                for (int i = 1; i < s.Length; i++)
                {
                    if (char.IsLetterOrDigit(s[i])) continue;
                    pos1 = i;
                    break;
                }
                result.Add(new TextColorItem(s[..pos1], TextColorEnum.ParameterColor));
                s = s[pos1..];
            }
            if (s.Length > 0)
            {
                result.Add(new TextColorItem(s, TextColorEnum.Default));
            }
        }
        else
        {
            result.Add(new TextColorItem(s, TextColorEnum.Default));
        }
        return result;
    }
}
