using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    /// <summary>
    /// Random number generator for DAGS scripts.
    /// </summary>
    private static readonly Random _random = new();

    /// <summary>
    /// Process a DAGS script.
    /// </summary>
    public static List<GrifMessage> Process(Grod grod, string? script)
    {
        List<GrifMessage> items = [new GrifMessage(MessageType.Text, script ?? "")];
        return ProcessItems(grod, items);
    }

    /// <summary>
    /// Process a list of DAGS items.
    /// </summary>
    public static List<GrifMessage> ProcessItems(Grod grod, List<GrifMessage> items)
    {
        List<GrifMessage> result = [];
        foreach (var item in items)
        {
            if (item.Type == MessageType.Error)
            {
                result.Add(item);
                continue;
            }
            if (item.Type == MessageType.Text ||
                item.Type == MessageType.Internal ||
                item.Type == MessageType.Script)
            {
                if (string.IsNullOrEmpty(item.Value))
                {
                    continue;
                }
                if (IsScript(item.Value))
                {
                    var script = CreateScript(item.Value);
                    do
                    {
                        var answer = ProcessOneCommand(grod, script);
                        if (answer.Count > 0)
                        {
                            result.AddRange(answer);
                        }
                        if (script.ReturnFlag)
                        {
                            break;
                        }
                    } while (script.Index < script.Tokens.Length);
                    continue;
                }
                // plain text
                result.Add(item);
                continue;
            }
            if (item.Type == MessageType.InChannel)
            {
                if (!IsNull(grod.Get(INCHANNEL, true)))
                {
                    throw new Exception("DagsInChannel value is not empty.");
                }
                grod.Set(INCHANNEL, item.Value);
                continue;
            }
            throw new Exception($"Unsupported DagsType: {item.Type}");
        }
        return result;
    }
}
