using System.Text;
using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    /// <summary>
    /// Handle @for...@endfor
    /// </summary>
    private static void HandleFor(Grod grod, ScriptObj script, List<GrifMessage> p, List<GrifMessage> result)
    {
        // @for(i,<start>,<end inclusive>)=...$i...@endfor
        var iterator = PARAM_CHAR + p[0].Value;
        var startIndex = script.Index;
        var level = 0;
        do
        {
            var token = script.Tokens[script.Index++];
            if (token.Equals(FOR_TOKEN, OIC))
            {
                level++;
            }
            else if (token.Equals(ENDFOR_TOKEN, OIC))
            {
                if (level <= 0)
                {
                    break;
                }
                level--;
            }
        } while (script.Index < script.Tokens.Length);
        var endIndex = script.Index - 2;
        var int1 = long.Parse(p[1].Value);
        var int2 = long.Parse(p[2].Value);
        for (long value = int1; value <= int2; value++)
        {
            List<string> loopTokens = [];
            for (int i = startIndex; i <= endIndex; i++)
            {
                var token = script.Tokens[i];
                if (token.Contains(iterator, OIC))
                {
                    token = token.Replace(iterator, value.ToString());
                }
                loopTokens.Add(token);
            }
            ScriptObj loopScript = new()
            {
                Tokens = [.. loopTokens],
                Index = 0,
                LocalData = script.LocalData,
            };
            do
            {
                var answer = ProcessOneCommand(grod, loopScript);
                if (answer.Count > 0)
                {
                    result.AddRange(answer);
                }
                if (loopScript.ReturnFlag)
                {
                    script.ReturnFlag = true;
                    break;
                }
            } while (loopScript.Index < loopScript.Tokens.Length);
            if (script.ReturnFlag)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Handle @foreachkey...@endforeachkey
    /// </summary>
    private static void HandleForEachKey(Grod grod, ScriptObj script, List<GrifMessage> p, List<GrifMessage> result)
    {
        // @foreachkey(i,prefix,[suffix])=...$i...@endforeachkey
        var newTokens = new StringBuilder();
        var level = 0;
        do
        {
            var token = script.Tokens[script.Index++];
            if (token.Equals(FOREACHKEY_TOKEN, OIC))
            {
                level++;
            }
            else if (token.Equals(ENDFOREACHKEY_TOKEN, OIC))
            {
                if (level <= 0)
                {
                    break;
                }
                level--;
            }
            if (newTokens.Length > 0)
            {
                newTokens.Append(' ');
            }
            newTokens.Append(token);
        } while (script.Index < script.Tokens.Length);
        var keys = grod.Keys(p[1].Value, true, true);
        foreach (string key in keys)
        {
            var value = key[p[1].Value.Length..];
            if (p.Count > 2)
            {
                if (!value.EndsWith(p[2].Value, OIC))
                {
                    continue;
                }
                value = value[..^p[2].Value.Length];
            }
            var loopText = newTokens.ToString().Replace($"{PARAM_CHAR}{p[0].Value}", value);
            var loopScript = CreateScript(loopText);
            loopScript.LocalData = script.LocalData;
            do
            {
                var answer = ProcessOneCommand(grod, loopScript);
                if (answer.Count > 0)
                {
                    result.AddRange(answer);
                }
                if (loopScript.ReturnFlag)
                {
                    break;
                }
            } while (loopScript.Index < loopScript.Tokens.Length);
        }
    }

    /// <summary>
    /// Handle @foreachlist...@endforeachlist
    /// </summary>
    private static void HandleForEachList(Grod grod, ScriptObj script, List<GrifMessage> p, List<GrifMessage> result)
    {
        // @foreachlist(x,listname)=...$x...@endforeachlist
        var newTokens = new StringBuilder();
        var level = 0;
        do
        {
            var token = script.Tokens[script.Index++];
            if (token.Equals(FOREACHLIST_TOKEN, OIC))
            {
                level++;
            }
            else if (token.Equals(ENDFOREACHLIST_TOKEN, OIC))
            {
                if (level <= 0)
                {
                    break;
                }
                level--;
            }
            if (newTokens.Length > 0)
            {
                newTokens.Append(' ');
            }
            newTokens.Append(token);
        } while (script.Index < script.Tokens.Length);
        // p[1] holds the name of the list
        string? list = GetGlobalOrLocal(grod, script, p[1].Value, true);
        if (!string.IsNullOrWhiteSpace(list))
        {
            var items = SplitList(list);
            foreach (string value in items)
            {
                var value2 = FixListItemOut(value);
                if (!string.IsNullOrEmpty(value2))
                {
                    var loopText = newTokens.ToString().Replace($"{PARAM_CHAR}{p[0].Value}", value2);
                    var loopScript = CreateScript(loopText);
                    loopScript.LocalData = script.LocalData;
                    do
                    {
                        var answer = ProcessOneCommand(grod, loopScript);
                        if (answer.Count > 0)
                        {
                            result.AddRange(answer);
                        }
                        if (loopScript.ReturnFlag)
                        {
                            break;
                        }
                    } while (loopScript.Index < loopScript.Tokens.Length);
                }
            }
        }
    }
}
