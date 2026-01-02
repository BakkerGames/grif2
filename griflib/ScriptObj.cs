namespace GrifLib;

public class ScriptObj
{
    public string[] Tokens { get; set; } = [];

    public int Index { get; set; } = 0;

    public bool ReturnFlag { get; set; } = false;

    public Grod LocalData { get; set; } = new();
}
