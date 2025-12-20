using System.Text;
using static Grif.Common;

namespace Grif;

public partial class Dags
{
    private static void HandleFor(List<GrifMessage> p, string[] tokens, ref int index, Grod grod, List<GrifMessage> result)
    {
        // @for(i,<start>,<end inclusive>)=...$i...@endfor
        var iterator = "$" + p[0].Value;
        var startIndex = index;
        var level = 0;
        do
        {
            var token = tokens[index++];
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
        } while (index < tokens.Length);
        var endIndex = index - 2;
        var int1 = long.Parse(p[1].Value);
        var int2 = long.Parse(p[2].Value);
        for (long value = int1; value <= int2; value++)
        {
            List<string> loopTokens = [];
            for (int i = startIndex; i <= endIndex; i++)
            {
                var token = tokens[i];
                if (token.Contains(iterator, OIC))
                {
                    token = token.Replace(iterator, value.ToString());
                }
                loopTokens.Add(token);
            }
            string[] loopArray = [.. loopTokens];
            var loopIndex = 0;
            do
            {
                var answer = ProcessOneCommand(loopArray, ref loopIndex, grod);
                if (answer.Count > 0)
                {
                    result.AddRange(answer);
                }
            } while (loopIndex < loopArray.Length);
        }
    }

    private static void HandleForEachKey(List<GrifMessage> p, string[] tokens, ref int index, Grod grod, List<GrifMessage> result)
    {
        // @foreachkey(i,prefix,[suffix])=...$i...@endforeachkey
        var newTokens = new StringBuilder();
        var level = 0;
        do
        {
            var token = tokens[index++];
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
        } while (index < tokens.Length);
        var keys = grod.Keys(p[1].Value, true, true);
        foreach (string key in keys)
        {
            var value = key[p[1].Value.Length..];
            if (p.Count > 2)
            {
                if (!value.EndsWith(p[2].Value, OIC))
                    continue;
                value = value[..^p[2].Value.Length];
            }
            var script = newTokens.ToString().Replace($"${p[0].Value}", value);
            result.AddRange(Process(grod, script));
        }
    }

    private static void HandleForEachList(List<GrifMessage> p, string[] tokens, ref int index, Grod grod, List<GrifMessage> result)
    {
        // @foreachlist(x,listname)=...$x...@endforeachlist
        var newTokens = new StringBuilder();
        var level = 0;
        do
        {
            var token = tokens[index++];
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
        } while (index < tokens.Length);
        // p[1] holds the name of the list
        string? list = grod.Get(p[1].Value, true);
        if (!string.IsNullOrWhiteSpace(list))
        {
            var items = SplitList(list);
            foreach (string value in items)
            {
                var value2 = FixListItemOut(value);
                if (!string.IsNullOrEmpty(value2))
                {
                    var script = newTokens.ToString().Replace($"${p[0].Value}", value2);
                    result.AddRange(Process(grod, script));
                }
            }
        }
    }
}
