using System.Text;
using static Grif.Common;

namespace Grif;

internal class Program
{
    private static readonly Queue<string> inputQueue = new();

    private static int outputCount = 0;
    private static int maxOutputWidth = 0;

    private static readonly List<string> fileList = [];
    private static string? inputFilename;
    private static string? splitInput;
    private static string? outputFilename;

    internal static async Task Main(string[] args)
    {
        var parseResult = ParseParameters(args);
        if (parseResult != 0)
        {
            Environment.Exit(parseResult);
            return;
        }
        // load data
        var game = new IFGame();
        Grod baseGrod = new(fileList[0]);
        baseGrod.AddItems(IO.ReadGrif(fileList[0]));
        for (int i = 1; i < fileList.Count; i++)
        {
            var newgrod = new Grod(fileList[i]);
            newgrod.AddItems(IO.ReadGrif(fileList[i]));
            newgrod.Parent = baseGrod;
            baseGrod = newgrod;
        }
        var gameName = baseGrod.Get(GAMENAME, true);
        if (string.IsNullOrWhiteSpace(gameName))
        {
            gameName = "Unnamed Game";
        }
        game.Initialize(baseGrod, gameName, null);
        if (inputFilename != null)
        {
            try
            {
                var inStream = File.ReadAllLines(inputFilename);
                foreach (var line in inStream)
                {
                    var tempLine = line;
                    if (tempLine.Contains("//"))
                    {
                        tempLine = tempLine[..tempLine.IndexOf("//")].Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(tempLine))
                    {
                        if (splitInput != null && tempLine.Contains(splitInput))
                        {
                            var splitLines = tempLine.Split(splitInput);
                            foreach (var splitLine in splitLines)
                            {
                                inputQueue.Enqueue(splitLine.Trim());
                            }
                        }
                        else
                        {
                            inputQueue.Enqueue(tempLine);
                        }
                    }
                }
            }
            catch (Exception)
            {
                OutputText($"Error opening input file: {inputFilename}");
                return;
            }
        }
        // check for max width setting
        maxOutputWidth = (int)(baseGrod.GetNumber("system.output_width", true) ?? 0);
        // start game loop
        game.InputEvent += Input;
        game.OutputEvent += Output;
        await game.Intro();
        await game.GameLoop();
    }

    #region Private Methods

    private static string Syntax()
    {
        StringBuilder result = new();
        result.AppendLine("GRIF - Game Runner for Interactive Fiction");
        result.AppendLine();
        result.AppendLine($"Version {IFGame.Version}");
        result.AppendLine();
        result.AppendLine("grif <filename.grif | directory>");
        result.AppendLine("     [-h  | --help | -?]");
        result.AppendLine("     [-i  | --input  <filename>]");
        result.AppendLine("     [-si | --split-input <splitchar>]");
        result.AppendLine("     [-o  | --output <filename>]");
        result.AppendLine("     [-m  | --mod    <filename.grif | directory>]");
        result.AppendLine();
        result.AppendLine("There may be multiple -m/--mod parameters.");
        return result.ToString();
    }

    private static int ParseParameters(string[] args)
    {
        if (args.Length == 0)
        {
            OutputText(Syntax());
            return 1;
        }
        int index = 0;
        while (index < args.Length)
        {
            if (args[index].StartsWith('-'))
            {
                if (index + 1 >= args.Length)
                {
                    OutputText($"Argument must have a value: {args[index]}");
                    OutputText(Syntax());
                    return 2;
                }
                if (args[index].Equals("-h", OIC) ||
                    args[index].Equals("--help", OIC) ||
                    args[index].Equals("-?"))
                {
                    OutputText(Syntax());
                    return 2;
                }
                else if (args[index].Equals("-i", OIC) ||
                    args[index].Equals("--input", OIC))
                {
                    index++;
                    inputFilename = args[index++];
                    if (!File.Exists(inputFilename))
                    {
                        OutputText($"Input file not found: {inputFilename}");
                        OutputText(Syntax());
                        return 2;
                    }
                }
                else if (args[index].Equals("-si", OIC) ||
                    args[index].Equals("--split-input", OIC))
                {
                    index++;
                    splitInput = args[index++];
                }
                else if (args[index].Equals("-o", OIC) ||
                    args[index].Equals("--output", OIC))
                {
                    index++;
                    var tempFilename = args[index++];
                    try
                    {
                        // check if file can be created
                        var outStream = File.CreateText(tempFilename);
                        outStream.Close();
                        outputFilename = tempFilename;
                    }
                    catch (Exception)
                    {
                        OutputText($"Error creating output file: {tempFilename}");
                        OutputText(Syntax());
                        return 2;
                    }
                }
                else if (args[index].Equals("-m", OIC) ||
                    args[index].Equals("--mod", OIC))
                {
                    index++;
                    var modFilename = args[index++];
                    if (File.Exists(modFilename))
                    {
                        fileList.Add(modFilename);
                    }
                    else if (File.Exists(modFilename + DATA_EXTENSION))
                    {
                        fileList.Add(modFilename + DATA_EXTENSION);
                    }
                    else if (Directory.Exists(modFilename))
                    {
                        foreach (string file in Directory.GetFiles(modFilename, "*" + DATA_EXTENSION))
                        {
                            fileList.Add(file);
                        }
                    }
                    else
                    {
                        OutputText($"File/directory not found: {modFilename}");
                    }
                }
                else
                {
                    OutputText($"Unknown argument: {args[index++]}");
                    OutputText(Syntax());
                    return 2;
                }
            }
            else
            {
                var filename = args[index++];
                if (!CheckFilename(filename))
                {
                    return 2;
                }
            }
        }
        if (fileList.Count == 0)
        {
            OutputText(Syntax());
            return 1;
        }
        return 0;
    }

    private static bool CheckFilename(string filename)
    {
        var extension = Path.GetExtension(filename);
        if (extension.Equals(STACK_EXTENSION, OIC))
        {
            return CheckStackFile(filename);
        }
        else if (extension.Equals(DATA_EXTENSION, OIC))
        {
            return CheckDataFile(filename);
        }
        else if (File.Exists(filename + STACK_EXTENSION))
        {
            return CheckStackFile(filename);
        }
        else if (File.Exists(filename + DATA_EXTENSION))
        {
            return CheckDataFile(filename);
        }
        else if (Directory.Exists(filename))
        {
            return CheckDirectoryFiles(filename);
        }
        else
        {
            OutputText($"File/directory not found: {filename}");
            return false;
        }
    }

    private static bool CheckStackFile(string filename)
    {
        var path = Path.GetDirectoryName(filename) ?? ".";
        var found = false;
        foreach (var line in File.ReadLines(filename))
        {
            var tempLine = line.Trim();
            if (tempLine.Length == 0 || tempLine.StartsWith("//"))
            {
                continue;
            }
            if (string.IsNullOrEmpty(Path.GetDirectoryName(tempLine))) {
                tempLine = Path.Combine(path, tempLine);
            }
            if (!CheckFilename(tempLine))
            {
                return false;
            }
            found = true;
        }
        return found;
    }

    private static bool CheckDataFile(string filename)
    {
        if (string.IsNullOrEmpty(Path.GetExtension(filename)))
        {
            filename += DATA_EXTENSION;
        }
        if (!File.Exists(filename))
        {
            return false;
        }
        if (!Path.GetExtension(filename).Equals(DATA_EXTENSION, OIC))
        {
            OutputText($"Data file must have {DATA_EXTENSION} extension: {filename}");
            return false;
        }
        fileList.Add(filename);
        return true;
    }

    private static bool CheckDirectoryFiles(string directory)
    {
        var stacks = Directory.GetFiles(directory, "*" + STACK_EXTENSION);
        var files = Directory.GetFiles(directory, "*" + DATA_EXTENSION);
        if (stacks.Length == 0 && files.Length == 0)
        {
            OutputText($"No stack or data files found in directory: {directory}");
            return false;
        }
        foreach (var stack in stacks)
        {
            if (!CheckStackFile(stack))
            {
                return false;
            }
        }
        foreach (var file in files)
        {
            if (!CheckDataFile(file))
            {
                return false;
            }
        }
        return true;
    }

    private static void Input(object sender)
    {
        OutputText(((IFGame)sender).Prompt() ?? "");
        string? input;
        if (inputQueue.Count > 0)
        {
            input = inputQueue.Dequeue();
            Console.WriteLine(input);
        }
        else
        {
            input = Console.ReadLine();
        }
        if (input != null)
        {
            OutputTextLog(input + Environment.NewLine);
            var message = new GrifMessage(MessageType.Text, input);
            ((IFGame)sender).InputMessages.Enqueue(message);
            OutputText(((IFGame)sender).AfterPrompt() ?? "");
        }
    }

    private static void Output(object sender, GrifMessage e)
    {
        OutputText(e.Value);
    }

    private static void OutputText(string text)
    {
        if (text.Contains("\\s"))
        {
            text = text.Replace("\\s", " ");
        }
        while (text.Contains("\\n"))
        {
            var index = text.IndexOf("\\n");
            var before = text[..index];
            text = text[(index + 2)..];
            var lines = Wordwrap(before);
            foreach (var line in lines)
            {
                Console.WriteLine(line);
                OutputTextLog(line + Environment.NewLine);
            }
            outputCount = 0;
        }
        if (!string.IsNullOrEmpty(text))
        {
            var lines = Wordwrap(text);
            for (int i = 0; i < lines.Count - 1; i++)
            {
                var line = lines[i];
                Console.WriteLine(line);
                OutputTextLog(line + Environment.NewLine);
            }
            var lastLine = lines[^1];
            Console.Write(lastLine);
            OutputTextLog(lastLine);
        }
    }

    private static List<string> Wordwrap(string text)
    {
        if (maxOutputWidth <= 0 || string.IsNullOrEmpty(text))
        {
            return [text];
        }
        List<string> result = [];
        StringBuilder currentLine = new();
        var words = text.Split(' ');
        foreach (var word in words)
        {
            if (outputCount + word.Length + 1 > maxOutputWidth)
            {
                // output current line
                result.Add(currentLine.ToString());
                currentLine.Clear();
                outputCount = 0;
            }
            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
                outputCount++;
            }
            currentLine.Append(word);
            outputCount += word.Length;
        }
        if (currentLine.Length > 0)
        {
            result.Add(currentLine.ToString());
        }
        return result;
    }

    private static void OutputTextLog(string text)
    {
        if (outputFilename == null)
        {
            return;
        }
        try
        {
            using var outStream = File.AppendText(outputFilename);
            outStream.Write(text);
            outStream.Flush();
        }
        catch (Exception)
        {
            // ignore file write errors
        }
    }

    #endregion
}
