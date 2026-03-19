namespace GrifLib;

/// <summary>
/// Colors used for the various DAGS script elements. The actual colors are defined in an external color palette and looked up using these enum values.
/// </summary>
public enum TextColorEnum
{
    Default = 0,
    PunctuationColor = 1,
    ParenthesisColor = 2,
    TokenColor = 3,
    IfColor = 4,
    ForColor = 5,
    QuoteColor = 6,
    ParameterColor = 7,
    CommentColor = 8,
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
