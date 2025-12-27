namespace GrifLib;

/// <summary>
/// Specifies the type of a message within the messaging system.
/// </summary>
public enum MessageType
{
    Error = -1,
    Unknown = 0,
    Text = 1,
    Script = 2,
    Internal = 3,
    OutChannel = 4,
    InChannel = 5,
}

/// <summary>
/// Represents a Grif message with a specified type, value, and optional extra value.
/// </summary>
public record GrifMessage(MessageType Type, string Value, string? ExtraValue = null);
