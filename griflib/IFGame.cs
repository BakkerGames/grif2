using static GrifLib.Common;
using static GrifLib.Dags;
using static GrifLib.IFParser;
using static GrifLib.IO;

namespace GrifLib;

/// <summary>
/// Represents the method that handles an input event raised by a component or control.
/// </summary>
public delegate void InputEventHandler(object sender);

/// <summary>
/// Represents the method that will handle an output event raised by the system.
/// </summary>
/// <param name="sender">The source of the event.</param>
public delegate void OutputEventHandler(object sender, GrifMessage e);

public class IFGame
{
    private Grod _baseGrod = new();
    private Grod _overlayGrod = new();
    private string _saveBasePath = "";
    private string? _referenceBasePath;

    /// <summary>
    /// Occurs when an input action is performed, such as a key press or mouse event.
    /// </summary>
    public event InputEventHandler? InputEvent;

    /// <summary>
    /// Occurs when output data is available or an output event is raised.
    /// </summary>
    public event OutputEventHandler? OutputEvent;

    /// <summary>
    /// Gets or sets a value indicating whether the game has ended.
    /// </summary>
    public bool GameOver { get; set; } = false;

    /// <summary>
    /// Gets the queue of input messages awaiting processing.
    /// </summary>
    public Queue<GrifMessage> InputMessages { get; } = new();

    /// <summary>
    /// Gets the queue of messages that are pending to be sent or processed by the system.
    /// </summary>
    public Queue<GrifMessage> OutputMessages { get; } = new();

    /// <summary>
    /// Initializes the game state, configuring save and reference paths and preparing the game data for use.
    /// </summary>
    public void Initialize(Grod grod, string saveBasePath, string? referenceBasePath = null)
    {
        GameOver = false;
        _saveBasePath = saveBasePath;
        _referenceBasePath = referenceBasePath;
        try
        {
            if (!Path.IsPathRooted(_saveBasePath))
            {
                _saveBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), APP_NAME, _saveBasePath);
            }
            if (!Directory.Exists(_saveBasePath))
            {
                Directory.CreateDirectory(_saveBasePath);
            }
        }
        catch (Exception)
        {
            throw new IOException("Failed to initialize game save path.");
        }
        try
        {
            if (!string.IsNullOrEmpty(_referenceBasePath))
            {
                if (!Path.IsPathRooted(_referenceBasePath))
                {
                    _referenceBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_NAME, _referenceBasePath);
                }
                if (!Directory.Exists(_referenceBasePath))
                {
                    throw new IOException("Reference path does not exist.");
                }
            }
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new IOException("Failed to initialize game resource path.");
        }
        try
        {
            _baseGrod = grod;
            _overlayGrod = new(SAVE_FILENAME, Path.Combine(_saveBasePath, SAVE_FILENAME + SAVE_EXTENSION))
            {
                Parent = _baseGrod
            };
        }
        catch (Exception)
        {
            throw new IOException("Failed to initialize game data.");
        }
    }

    /// <summary>
    /// Processes and outputs the introductory message or script for the overlay.
    /// </summary>
    public async Task Intro()
    {
        var intro = await Task.Run(() => _overlayGrod.Get(INTRO, true));
        if (IsScript(intro))
        {
            var introItems = await Task.Run(() => Process(_overlayGrod, intro));
            foreach (var item in introItems)
            {
                OutputMessages.Enqueue(item);
            }
        }
        else
        {
            OutputMessages.Enqueue(new GrifMessage(MessageType.Text, intro ?? ""));
        }
        while (OutputMessages.Count > 0)
        {
            var outputMessage = OutputMessages.Dequeue();
            ProcessOutputMessage(outputMessage);
        }
    }

    /// <summary>
    /// Runs the main asynchronous game loop, processing input and output messages and advancing the game state until the game is over.
    /// </summary>
    public async Task GameLoop()
    {
        while (true)
        {
            if (GameOver)
            {
                break;
            }
            AdvanceGameState();
            while (!GameOver && OutputMessages.Count > 0)
            {
                var outputMessage = OutputMessages.Dequeue();
                ProcessOutputMessage(outputMessage);
            }
            if (!GameOver && InputEvent != null && InputMessages.Count == 0)
            {
                InputEvent?.Invoke(this);
            }
            while (!GameOver && InputMessages.Count == 0)
            {
                await Task.Delay(100);
            }
            if (!GameOver && InputMessages.Count > 0)
            {
                var inputMessage = InputMessages.Dequeue();
                ProcessInputMessage(inputMessage);
            }
            while (!GameOver && OutputMessages.Count > 0)
            {
                var outputMessage = OutputMessages.Dequeue();
                ProcessOutputMessage(outputMessage);
            }
        }
    }

    /// <summary>
    /// Generates and returns the current prompt text, processing scripts if present.
    /// </summary>
    public string? Prompt()
    {
        var prompt = _overlayGrod.Get(PROMPT, true);
        if (IsScript(prompt))
        {
            var promptData = Process(_overlayGrod, prompt);
            prompt = "";
            foreach (var item in promptData)
            {
                prompt += item.Value;
            }
        }
        return prompt;
    }

    /// <summary>
    /// Generates and returns the text or script to be displayed after the prompt.
    /// </summary>
    public string? AfterPrompt()
    {
        var afterPrompt = _overlayGrod.Get(AFTER_PROMPT, true);
        if (IsScript(afterPrompt))
        {
            afterPrompt = Process(_overlayGrod, afterPrompt).FirstOrDefault()?.Value;
        }
        return afterPrompt;
    }

    #region Private routines

    /// <summary>
    /// Processes the specified input message and enqueues the resulting items for output.
    /// </summary>
    private void ProcessInputMessage(GrifMessage inputMessage)
    {
        var inputItems = ParseInput(_overlayGrod, inputMessage.Value);
        foreach (var item in inputItems ?? [])
        {
            OutputMessages.Enqueue(item);
        }
    }

    /// <summary>
    /// Processes an output message and dispatches it to the appropriate handler based on its type.
    /// </summary>
    private void ProcessOutputMessage(GrifMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Text:
                OutputEvent?.Invoke(this, message);
                break;
            case MessageType.OutChannel:
                HandleOutChannel(message);
                break;
            case MessageType.Script:
                var outputItems = ProcessItems(_overlayGrod, [message]);
                foreach (var item in outputItems)
                {
                    OutputMessages.Enqueue(item);
                }
                break;
            case MessageType.Error:
                OutputEvent?.Invoke(this, message);
                break;
            default:
                OutputEvent?.Invoke(this, new GrifMessage(MessageType.Text, $"Unsupported output message type: {message.Type}", message.Value + message.ExtraValue));
                break;
        }
    }

    /// <summary>
    /// Advances the game state by processing background scripts and enqueuing resulting output messages.
    /// </summary>
    private void AdvanceGameState()
    {
        var keys = _overlayGrod.Keys(BACKGROUND_PREFIX, true, false);
        foreach (var key in keys)
        {
            var script = $"{SCRIPT_TOKEN}{key})";
            var items = Process(_overlayGrod, script);
            foreach (var item in items)
            {
                OutputMessages.Enqueue(item);
            }
        }
    }

    /// <summary>
    /// Handles an outbound channel message by performing the corresponding action.
    /// </summary>
    private void HandleOutChannel(GrifMessage message)
    {
        bool exists;
        if (message.Value.Equals(OUTCHANNEL_GAMEOVER, OIC))
        {
            GameOver = true;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_EXISTS_SAVE, OIC))
        {
            var savefile = Path.Combine(_saveBasePath, SAVE_FILENAME + SAVE_EXTENSION);
            exists = File.Exists(savefile);
            _overlayGrod.Set(INCHANNEL, exists ? "true" : "false");
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_EXISTS_SAVE_NAME, OIC))
        {
            if (message.ExtraValue == null)
            {
                throw new Exception("Save filename not specified.");
            }
            var savefile = Path.Combine(_saveBasePath, message.ExtraValue + SAVE_EXTENSION);
            exists = File.Exists(savefile);
            _overlayGrod.Set(INCHANNEL, exists ? "true" : "false");
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_SAVE, OIC))
        {
            var savefile = Path.Combine(_saveBasePath, SAVE_FILENAME + SAVE_EXTENSION);
            var itemList = _overlayGrod.Items(false, true);
            WriteGrif(savefile, itemList, true);
            _overlayGrod.Changed = false;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_SAVE_NAME, OIC))
        {
            if (message.ExtraValue == null)
            {
                throw new Exception("Save filename not specified.");
            }
            var savefile = Path.Combine(_saveBasePath, message.ExtraValue + SAVE_EXTENSION);
            var itemList = _overlayGrod.Items(false, true);
            WriteGrif(savefile, itemList, true);
            _overlayGrod.Changed = false;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_RESTORE, OIC))
        {
            var savefile = Path.Combine(_saveBasePath, SAVE_FILENAME + SAVE_EXTENSION);
            if (!File.Exists(savefile))
            {
                throw new FileNotFoundException(savefile);
            }
            var itemList = ReadGrif(savefile);
            _overlayGrod.Clear(false); // clear only the user data
            _overlayGrod.AddItems(itemList);
            _overlayGrod.Changed = false;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_RESTORE_NAME, OIC))
        {
            if (message.ExtraValue == null)
            {
                throw new Exception("Save filename not specified.");
            }
            var savefile = Path.Combine(_saveBasePath, message.ExtraValue + SAVE_EXTENSION);
            if (!File.Exists(savefile))
            {
                throw new FileNotFoundException(savefile);
            }
            var itemList = ReadGrif(savefile);
            _overlayGrod.Clear(false); // clear only the user data
            _overlayGrod.AddItems(itemList);
            _overlayGrod.Changed = false;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_RESTART, OIC))
        {
            _overlayGrod.Clear(false); // clear only the user data
            _overlayGrod.Changed = false;
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_ASK, OIC))
        {
            if (InputEvent != null && InputMessages.Count == 0)
            {
                InputEvent?.Invoke(this);
            }
            while (InputMessages.Count == 0)
            {
                Thread.Sleep(100);
            }
            var inputMessage = InputMessages.Dequeue();
            _overlayGrod.Set(INCHANNEL, inputMessage.Value);
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_ENTER, OIC))
        {
            if (InputEvent != null && InputMessages.Count == 0)
            {
                InputEvent?.Invoke(this);
            }
            while (InputMessages.Count == 0)
            {
                Thread.Sleep(100);
            }
            _ = InputMessages.Dequeue();
            return;
        }
        if (message.Value.Equals(OUTCHANNEL_ADD_EXTRA, OIC))
        {
            // Add a new grod to the hierarchy for storing extra data, if it doesn't already exist.
            if (message.ExtraValue == null)
            {
                throw new Exception("Grod name not specified.");
            }
            var grodName = message.ExtraValue;
            var tempGrod = _overlayGrod.GetGrod(grodName);
            if (tempGrod == null)
            {
                // Insert a new grod into the hierarchy to save the data
                tempGrod = new Grod()
                {
                    Name = grodName,
                    FilePath = Path.Combine(_saveBasePath, grodName + DATA_EXTENSION),
                    Parent = _overlayGrod.Parent
                };
                _overlayGrod.Parent = tempGrod;
                if (File.Exists(tempGrod.FilePath))
                {
                    var itemList = ReadGrif(tempGrod.FilePath);
                    tempGrod.AddItems(itemList);
                    tempGrod.Changed = false;
                }
            }
        }
        if (message.Value.Equals(OUTCHANNEL_SET_EXTRA_VALUE, OIC))
        {
            if (message.ExtraValue == null)
            {
                throw new Exception("Grod name not specified.");
            }
            // extraParams = grodname \t key \t value
            var extraParms = message.ExtraValue.Split('\t');
            var grodName = extraParms[0];
            var tempGrod = _overlayGrod.GetGrod(grodName);
            if (tempGrod != null)
            {
                // Refresh the data in an extra grod from its file, if it exists.
                // This allows the game to keep the data in sync with external changes.
                if (tempGrod.FilePath == null)
                {
                    throw new Exception("Grod file path not specified.");
                }
                if (File.Exists(tempGrod.FilePath))
                {
                    var existingList = ReadGrif(tempGrod.FilePath);
                    tempGrod.Clear(false);
                    tempGrod.AddItems(existingList);
                    tempGrod.Changed = false;
                }
                // set value
                tempGrod.Set(extraParms[1], extraParms[2]); // key and value
                // save file immediately
                var savefile = tempGrod.FilePath;
                var itemList = tempGrod.Items(false, true);
                WriteGrif(savefile, itemList, true);
                tempGrod.Changed = false;
            }
        }
        if (IsScript(message.Value))
        {
            var outputItems = ProcessItems(_overlayGrod, [new GrifMessage(MessageType.Script, message.Value)]);
            foreach (var outputItem in outputItems)
            {
                OutputMessages.Enqueue(outputItem);
            }
            return;
        }
        // Sent unknown outchannel message to calling program
        OutputEvent?.Invoke(this, message);
    }

    #endregion
}
