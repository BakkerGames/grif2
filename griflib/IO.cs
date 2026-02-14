using System.Text;
using static GrifLib.Common;
using static GrifLib.Dags;

namespace GrifLib;

/// <summary>
/// Handles input/output operations for GRIF files.
/// </summary>
public static class IO
{
    /// <summary>
    /// Byte representation of a CR/LF newline.
    /// </summary>
    private static readonly byte[] NL_BYTES = [13, 10];

    /// <summary>
    /// Returns the full path to a save directory within the user's Documents folder.
    /// </summary>
    public static string GetSavePath(string filebase)
    {
        var result = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        result = Path.Combine(result, APP_NAME);
        result = Path.Combine(result, filebase);
        if (!Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }
        return result;
    }

    /// <summary>
    /// Opens a GRIF file, stack, or directory and returns the corresponding Grod object(s).
    /// </summary>
    public static Grod? OpenFile(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Filename cannot be null or whitespace.", nameof(filename));
        }
        if (Directory.Exists(filename))
        {
            Grod? baseGrod = null;
            foreach (var file in Directory.GetFiles(filename, "*" + DATA_EXTENSION))
            {
                var grod = OpenGrifFile(file);
                if (baseGrod == null)
                {
                    baseGrod = grod;
                }
                else
                {
                    grod.Parent = baseGrod;
                    baseGrod = grod;
                }
            }
            return baseGrod;
        }
        if (!Path.HasExtension(filename))
        {
            filename += DATA_EXTENSION;
        }
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("The specified file does not exist.", filename);
        }
        if (Path.GetExtension(filename).Equals(DATA_EXTENSION, OIC) ||
            Path.GetExtension(filename).Equals(SAVE_EXTENSION, OIC))
        {
            return OpenGrifFile(filename);
        }
        else if (Path.GetExtension(filename).Equals(STACK_EXTENSION, OIC))
        {
            return OpenGrifStack(filename);
        }
        else
        {
            throw new NotSupportedException("Unsupported file format.");
        }
    }

    /// <summary>
    /// Reads a GRIF file and returns a list of GrodItems.
    /// </summary>
    public static List<GrodItem> ReadGrif(string filePath)
    {
        using var reader = new StreamReader(filePath);
        return ReadGrif(reader);
    }

    /// <summary>
    /// Reads a GRIF file from a StreamReader and returns a list of GrodItems.
    /// </summary>
    public static List<GrodItem> ReadGrif(StreamReader stream)
    {
        List<GrodItem> items = [];
        var jsonFormat = false;
        var content = stream.ReadToEnd();
        int index = 0;
        string key;
        string value;
        SkipWhitespace(content, ref index);
        if (index < content.Length && content[index] == '{')
        {
            jsonFormat = true;
            index++;
        }
        while (index < content.Length)
        {
            if (jsonFormat)
            {
                SkipWhitespace(content, ref index);
                if (index < content.Length)
                {
                    if (content[index] == '}')
                    {
                        index++;
                        break;
                    }
                    if (content[index] == ',')
                    {
                        index++;
                        SkipWhitespace(content, ref index);
                    }
                }
                (key, value) = ParseJsonKeyValue(content, ref index);
            }
            else
            {
                (key, value) = ParseGrifKeyValue(content, ref index);
            }
            if (!string.IsNullOrWhiteSpace(key))
            {
                items.Add(new(key, value));
            }
        }
        return items;
    }

    /// <summary>
    /// Writes a list of GrodItems to a GRIF file.
    /// </summary>
    public static void WriteGrif(string filePath, List<GrodItem> items, bool jsonFormat)
    {
        using var stream = GetGrifStream(filePath, items, jsonFormat);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        stream.Position = 0;
        stream.CopyTo(fileStream);
    }

    /// <summary>
    /// Gets a Stream containing the GRIF representation of the provided GrodItems.
    /// </summary>
    public static Stream GetGrifStream(string filePath, List<GrodItem> items, bool jsonFormat)
    {
        var writer = new MemoryStream();
        var needsComma = false;
        string value;
        writer.Write(Encoding.UTF8.GetBytes(HeaderComment(filePath, jsonFormat)));
        if (jsonFormat)
        {
            writer.Write(Encoding.UTF8.GetBytes("{"));
            writer.Write(NL_BYTES);
        }
        foreach (var item in items)
        {
            if (jsonFormat)
            {
                if (needsComma)
                {
                    writer.Write(Encoding.UTF8.GetBytes(","));
                    writer.Write(NL_BYTES);
                }
                writer.Write(Encoding.UTF8.GetBytes("\t\""));
                writer.Write(Encoding.UTF8.GetBytes(EncodeString(item.Key)));
                writer.Write(Encoding.UTF8.GetBytes("\":"));
            }
            else
            {
                writer.Write(Encoding.UTF8.GetBytes(EncodeString(item.Key)));
                writer.Write(NL_BYTES);
            }
            if (IsScript(item.Value))
            {
                // If the value starts with '@', it is a script
                if (jsonFormat)
                {
                    writer.Write(Encoding.UTF8.GetBytes(" \""));
                    value = CompressScript(item.Value);
                    writer.Write(Encoding.UTF8.GetBytes(EncodeString(value)));
                    writer.Write(Encoding.UTF8.GetBytes("\""));
                    needsComma = true;
                }
                else
                {
                    writer.Write(Encoding.UTF8.GetBytes(PrettyScript(item.Value, true)));
                    writer.Write(NL_BYTES);
                }
            }
            else
            {
                value = item.Value ?? NULL;
                if (value.Contains('\r') || value.Contains('\n'))
                {
                    value = value.Replace("\r", "").Replace("\n", NL_CHAR);
                }
                if (jsonFormat)
                {
                    writer.Write(Encoding.UTF8.GetBytes(" \""));
                    writer.Write(Encoding.UTF8.GetBytes(EncodeString(value)));
                    writer.Write(Encoding.UTF8.GetBytes("\""));
                    needsComma = true;
                }
                else
                {
                    if (value == "")
                    {
                        value = "\"\"";
                    }
                    else
                    {
                        if (value.StartsWith(' '))
                        {
                            value = SPACE_CHAR + value[1..];
                        }
                        if (value.EndsWith(' '))
                        {
                            value = value[..^1] + SPACE_CHAR;
                        }
                        writer.Write(Encoding.UTF8.GetBytes($"\t{value}"));
                        writer.Write(NL_BYTES);
                    }
                }
            }
        }
        if (jsonFormat)
        {
            writer.Write(NL_BYTES);
            writer.Write(Encoding.UTF8.GetBytes("}"));
        }
        return writer;
    }

    /// <summary>
    /// Wordwrap text according to maxOutputWidth.
    /// currLinePos is the count of chars on the line so far, zero for start of line.
    /// Add a newline after each line except the last.
    /// </summary>
    public static List<string> Wordwrap(string text, int currLinePos, int maxOutputWidth)
    {
        if (maxOutputWidth <= 0 ||
            string.IsNullOrEmpty(text) ||
            text.Length <= maxOutputWidth - currLinePos)
        {
            return [text];
        }
        List<string> result = [];
        int startPos = 0;
        int endPos = maxOutputWidth - currLinePos;
        while (text.Length - startPos > maxOutputWidth)
        {
            while (endPos > startPos)
            {
                if (text[endPos] == ' ')
                {
                    result.Add(text[startPos..endPos].TrimEnd());
                    startPos = endPos + 1;
                    while (startPos < text.Length && text[startPos] == ' ')
                    {
                        startPos++; // skip extra spaces
                    }
                    endPos = Math.Min(startPos + maxOutputWidth, text.Length - 1);
                    break;
                }
                endPos--;
            }
        }
        if (startPos < text.Length)
        {
            result.Add(text[startPos..]);
        }
        return result;
    }

    #region Private

    /// <summary>
    /// Opens a GRIF stack file and returns the corresponding Grod object(s).
    /// </summary>
    private static Grod? OpenGrifStack(string filename)
    {
        Grod? baseGrod = null;
        Grod grod;
        var path = Path.GetDirectoryName(filename) ?? ".";
        foreach (var line in File.ReadLines(filename))
        {
            var tempLine = line.Trim();
            if (tempLine.Length == 0 || tempLine.StartsWith("//"))
            {
                continue;
            }
            if (string.IsNullOrEmpty(Path.GetDirectoryName(tempLine)))
            {
                tempLine = Path.Combine(path, tempLine);
            }
            if (Directory.Exists(tempLine))
            {
                foreach (var file in Directory.GetFiles(tempLine, "*" + DATA_EXTENSION))
                {
                    grod = OpenGrifFile(file);
                    if (baseGrod == null)
                    {
                        baseGrod = grod;
                    }
                    else
                    {
                        grod.Parent = baseGrod;
                        baseGrod = grod;
                    }
                }
                continue;
            }
            if (!Path.HasExtension(tempLine))
            {
                tempLine += DATA_EXTENSION;
            }
            grod = OpenGrifFile(tempLine);
            if (baseGrod == null)
            {
                baseGrod = grod;
            }
            else
            {
                grod.Parent = baseGrod;
                baseGrod = grod;
            }
        }
        return baseGrod;
    }

    /// <summary>
    /// Opens a GRIF file and returns the corresponding Grod object.
    /// </summary>
    private static Grod OpenGrifFile(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Filename cannot be null or whitespace.", nameof(filename));
        }
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("The specified file does not exist.", filename);
        }
        var items = ReadGrif(filename);
        var grod = new Grod(Path.GetFileNameWithoutExtension(filename), filename);
        grod.AddItems(items);
        grod.Changed = false;
        return grod;
    }

    /// <summary>
    /// Encodes a string for JSON output, escaping special characters as needed.
    /// </summary>
    private static string EncodeString(string value)
    {
        StringBuilder result = new();
        var isScript = IsScript(value);
        foreach (char c in value)
        {
            if (c < ' ' || c > '~')
            {
                if (isScript && (c == '\r' || c == '\n' || c == '\t'))
                {
                    result.Append(c);
                }
                else if (c == '\r')
                {
                    result.Append(@"\r");
                }
                else if (c == '\n')
                {
                    result.Append(@"\n");
                }
                else if (c == '\t')
                {
                    result.Append(@"\t");
                }
                else
                {
                    result.Append(@"\u");
                    result.Append($"{(int)c:x4}");
                }
            }
            else if (c == '"' || c == '\\')
            {
                result.Append('\\');
                result.Append(c);
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Gets a JSON string value from the content starting at the given index.
    /// </summary>
    private static string GetJsonString(string content, ref int index)
    {
        if (index >= content.Length || content[index] != '\"')
        {
            throw new FormatException("Expected '\"' at the start of JSON string.");
        }
        index++; // Skip past opening quote
        StringBuilder result = new();
        bool lastSlash = false;
        while (index < content.Length)
        {
            char c = content[index++];
            if (lastSlash)
            {
                switch (c)
                {
                    case 'n':
                    case 'N':
                        result.Append('\n');
                        break;
                    case 't':
                    case 'T':
                        result.Append('\t');
                        break;
                    case 'r':
                    case 'R':
                        result.Append('\r');
                        break;
                    case 'b':
                    case 'B':
                        result.Append('\b');
                        break;
                    case 'f':
                    case 'F':
                        result.Append('\f');
                        break;
                    case 'u':
                    case 'U':
                        // Expecting 4 hex digits
                        if (index + 3 < content.Length &&
                            Uri.IsHexDigit(content[index]) &&
                            Uri.IsHexDigit(content[index + 1]) &&
                            Uri.IsHexDigit(content[index + 2]) &&
                            Uri.IsHexDigit(content[index + 3]))
                        {
                            var hex = content.Substring(index, 4);
                            if (long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out long codePoint))
                            {
                                result.Append((char)codePoint);
                                index += 4;
                            }
                            else
                            {
                                throw new FormatException("Invalid unicode escape sequence in JSON string.");
                            }
                        }
                        else
                        {
                            throw new FormatException("Invalid unicode escape sequence in JSON string.");
                        }
                        break;
                    case '"':
                        result.Append('\"');
                        break;
                    case '\\':
                        result.Append('\\');
                        break;
                    default:
                        result.Append('\\').Append(c);
                        break;
                }
                lastSlash = false;
            }
            else if (c == '\\')
            {
                lastSlash = true;
            }
            else if (c == '\"')
            {
                // End of string
                return result.ToString();
            }
            else
            {
                result.Append(c);
            }
        }
        throw new FormatException("Unterminated string in JSON object.");
    }

    /// <summary>
    /// Generates a header comment for the GRIF file.
    /// </summary>
    private static string HeaderComment(string path, bool jsonFormat)
    {
        StringBuilder result = new();
        result.Append("// ");
        result.Append(Path.GetFileName(path));
        result.Append(" - ");
        result.Append(jsonFormat ? "JSON format" : "GRIF format");
        result.AppendLine();
        return result.ToString();
    }

    /// <summary>
    /// Parses a key-value pair from GRIF format content starting at the given index.
    /// </summary>
    private static (string key, string value) ParseGrifKeyValue(string content, ref int index)
    {
        var needSpace = false;
        StringBuilder key = new();
        StringBuilder value = new();
        while (index < content.Length)
        {
            if (content[index] == '\r' || content[index] == '\n')
            {
                break;
            }
            key.Append(content[index++]);
        }
        while (index < content.Length && (content[index] == '\r' || content[index] == '\n'))
        {
            index++;
        }
        while (index < content.Length && (content[index] == '\t' || content[index] == ' '))
        {
            while (index < content.Length && (content[index] == '\t' || content[index] == ' '))
            {
                index++;
            }
            if (needSpace)
            {
                value.Append(' ');
            }
            while (index < content.Length && content[index] != '\r' && content[index] != '\n')
            {
                value.Append(content[index++]);
            }
            while (index < content.Length && (content[index] == '\r' || content[index] == '\n'))
            {
                index++;
            }
            needSpace = true;
        }
        var valueTemp = value.ToString().Trim();
        // change leading and trailing "\s" to spaces
        if (valueTemp.StartsWith("\\s"))
        {
            valueTemp = ' ' + valueTemp[2..];
        }
        if (valueTemp.EndsWith("\\s"))
        {
            valueTemp = valueTemp[..^2] + ' ';
        }
        return (key.ToString(), valueTemp);
    }

    /// <summary>
    /// Parses a key-value pair from JSON format content starting at the given index.
    /// </summary>
    private static (string key, string value) ParseJsonKeyValue(string content, ref int index)
    {
        SkipWhitespace(content, ref index);
        string key = GetJsonString(content, ref index);
        SkipWhitespace(content, ref index);
        if (index >= content.Length || content[index] != ':')
        {
            throw new FormatException("Expected ':' after key in JSON object.");
        }
        index++; // Skip past ':'
        SkipWhitespace(content, ref index);
        string value = GetJsonString(content, ref index);
        return (key, value);
    }

    #endregion
}
