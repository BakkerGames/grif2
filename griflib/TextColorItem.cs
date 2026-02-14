namespace GrifLib;

/// <summary>
/// Colors used for the various DAGS script elements. The actual colors are defined in an external color palette and looked up using these enum values.
/// </summary>
public enum TextColorEnum
{
    Default = 0,
    PunctuationColor = 1,
    TokenColor = 2,
    IfColor = 3,
    ForColor = 4,
    QuoteColor = 5,
    ParameterColor = 6,
    CommentColor = 7,
}

/// <summary>
/// Represents a text item with an associated color value.
/// </summary>
public class TextColorItem(string text, TextColorEnum colorValue)
{
    /// <summary>
    /// Text to be displayed
    /// </summary>
    public string Text { get; set; } = text;

    /// <summary>
    /// Color value for lookup in the external color palette
    /// </summary>
    public TextColorEnum ColorValue { get; set; } = colorValue;
}
