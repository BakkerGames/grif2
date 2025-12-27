namespace GrifLib;

public static partial class Common
{
    /// <summary>
    /// Skip whitespace and comments in a string.
    /// </summary>
    public static void SkipWhitespace(string content, ref int index)
    {
        bool found;
        do
        {
            found = false;
            while (index < content.Length && char.IsWhiteSpace(content[index]))
            {
                index++;
                found = true;
            }
            if (index + 1 < content.Length && content[index] == '/' && content[index + 1] == '/')
            {
                // Single line comment
                index += 2;
                while (index < content.Length && content[index] != '\n')
                {
                    index++;
                }
                found = true;
            }
            if (index + 1 < content.Length && content[index] == '/' && content[index + 1] == '*')
            {
                // Multi-line comment
                index += 2;
                while (index + 1 < content.Length && !(content[index] == '*' && content[index + 1] == '/'))
                {
                    index++;
                }
                if (index + 1 < content.Length)
                {
                    index += 2; // Skip past the closing */
                }
                found = true;
            }
        } while (found);
    }
}
