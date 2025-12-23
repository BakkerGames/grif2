using System.Text;
using static Grif.Common;

namespace Grif;

internal class Program
{
    private static readonly Queue<string> inputQueue = new();

    private static int outputCount = 0;
    private static int maxOutputWidth = 0;
    private static bool uppercaseInput = false;

    private static string? inputFilename;
    private static string? splitInput;
    private static string? outputFilename;

    internal static async Task Main(string[] args)
    {
        Grod baseGrod = new();
        var parseResult = ParseParameters(args, ref baseGrod);
        if (baseGrod == null)
        {
            Environment.Exit(1);
            return;
        }
        if (parseResult != 0)
        {
            Environment.Exit(parseResult);
            return;
        }
        // load data
        var game = new IFGame();
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
        // get settings
        maxOutputWidth = (int)(baseGrod.GetNumber(OUTPUT_WIDTH, true) ?? 0);
        uppercaseInput = baseGrod.GetBool(UPPERCASE, true) ?? false;
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
        result.AppendLine("grif <filename.grif | filename.grifstack | directory>");
        result.AppendLine("     [-h  | --help | -?]");
        result.AppendLine("     [-i  | --input  <filename>]");
        result.AppendLine("     [-si | --split-input <splitchar>]");
        result.AppendLine("     [-o  | --output <filename>]");
        result.AppendLine("     [-m  | --mod    <filename.grif | directory>]");
        result.AppendLine();
        result.AppendLine("There may be multiple -m/--mod parameters.");
        return result.ToString();
    }

    private static int ParseParameters(string[] args, ref Grod baseGrod)
    {
        if (args.Length == 0)
        {
            OutputText(Syntax());
            return 1;
        }
        try
        {
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
                        var grod = IO.OpenFile(modFilename); // to check if valid
                        if (grod == null)
                        {
                            OutputText($"Error opening mod file: {modFilename}");
                            OutputText(Syntax());
                            return 2;
                        }
                        if (baseGrod == null || baseGrod.Count(false) == 0)
                        {
                            baseGrod = grod;
                        }
                        else
                        {
                            grod.Parent = baseGrod;
                            baseGrod = grod;
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
                    var grod = IO.OpenFile(filename);
                    if (grod == null)
                    {
                        OutputText($"Error opening file: {filename}");
                        OutputText(Syntax());
                        return 2;
                    }
                    if (baseGrod == null || baseGrod.Count(false) == 0)
                    {
                        baseGrod = grod;
                    }
                    else
                    {
                        grod.Parent = baseGrod;
                        baseGrod = grod;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            OutputText($"Error processing parameters: {ex.Message}");
        }
        if (baseGrod == null || baseGrod.Count(false) == 0)
        {
            OutputText(Syntax());
            return 1;
        }
        return 0;
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
            if (uppercaseInput)
            {
                input = input.ToUpper();
            }
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
