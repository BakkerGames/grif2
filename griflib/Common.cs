namespace GrifLib;

public static partial class Common
{
    /// <summary>
    /// Gets the current version of the library.
    /// </summary>
    public static string Version { get { return "2.2026.1.26"; } }

    /// <summary>
    /// String comparison: OrdinalIgnoreCase
    /// </summary>
    public static readonly StringComparison OIC = StringComparison.OrdinalIgnoreCase;

    // Basic constants
    public const string NULL = "null";
    public const string TRUE = "true";
    public const string FALSE = "false";

    // Special characters
    public const char SCRIPT_CHAR = '@';
    public const string NL_CHAR = "\\n";
    public const string TAB_CHAR = "\\t";
    public const string SPACE_CHAR = "\\s";
    public const string COMMA_CHAR = "\\u002c";
    public const string LOCAL_CHAR = "__";

    // File and application constants
    public const string APP_NAME = "GRIF";
    public const string DATA_EXTENSION = ".grif";
    public const string SAVE_FILENAME = "save";
    public const string SAVE_EXTENSION = ".grifsave";
    public const string STACK_EXTENSION = ".grifstack";

    // System variable keys
    public const string AFTER_PROMPT = "system.after_prompt";
    public const string ARTICLE_KEY = "system.articles";
    public const string DONT_UNDERSTAND = "system.dont_understand";
    public const string GAMENAME = "system.gamename";
    public const string GAMETITLE = "system.gametitle";
    public const string GAMEOVER = "system.gameover";
    public const string INTRO = "system.intro";
    public const string OUTPUT_TAB_LENGTH = "system.tab_length";
    public const string OUTPUT_WIDTH = "system.output_width";
    public const string PROMPT = "system.prompt";
    public const string UPPERCASE = "system.uppercase";
    public const string DEBUG_FLAG = "system.debug";
    public const string WORDSIZE = "system.wordsize";
    public const string VERSION = "system.version";

    // Inchannel message prefix
    public const string INCHANNEL = "#INCHANNEL;";

    // Outchannel message prefixes
    public const string OUTCHANNEL_ASK = "#ASK;";
    public const string OUTCHANNEL_ENTER = "#ENTER;";
    public const string OUTCHANNEL_EXISTS_SAVE = "#EXISTS;";
    public const string OUTCHANNEL_EXISTS_SAVE_NAME = "#EXISTSNAME;";
    public const string OUTCHANNEL_GAMEOVER = "#GAMEOVER;";
    public const string OUTCHANNEL_RESTART = "#RESTART;";
    public const string OUTCHANNEL_RESTORE = "#RESTORE;";
    public const string OUTCHANNEL_RESTORE_NAME = "#RESTORENAME;";
    public const string OUTCHANNEL_SAVE = "#SAVE;";
    public const string OUTCHANNEL_SAVE_NAME = "#SAVENAME;";

    // Prefixes for word types
    public const string ADJECTIVE_PREFIX = "adjective.";
    public const string BACKGROUND_PREFIX = "background.";
    public const string COMMAND_PREFIX = "command.";
    public const string DIRECTION_PREFIX = "direction.";
    public const string NOUN_PREFIX = "noun.";
    public const string NOUNITEM_PREFIX = "nounitem.";
    public const string PREPOSITION_PREFIX = "preposition.";
    public const string VERB_PREFIX = "verb.";
}
