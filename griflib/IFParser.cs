using static GrifLib.Common;
using static GrifLib.Dags;

namespace GrifLib;

/*
Can handle the following input patterns:
    verb
    direction
    verb direction
    verb [article] [adjective...] noun
    verb [article] [adjective...] noun preposition [article] [adjective...] indirectnoun
    verb preposition [article] [adjective...] noun
Verbs may be in any position, e.g. "go north" or "north go".
Examples:
    "take lamp"
    "go north"
    "put lamp on table"
    "look at painting"
    "examine the small red box"
    "open door with key"
    "1234"
The constructed command is looked up in the Grod data to see if it exists.
*/

/// <summary>
/// Represents a parsed input pattern consisting of a key and its associated values.
/// The values are normalized strings and resized to _maxWordLen as needed.
/// </summary>
internal class ParserItem(string key, string[] values)
{
    public string Key { get; } = key;
    public string[] Values { get; } = values;
}

/// <summary>
/// Provides functionality to parse user input commands in an interactive fiction game.
/// </summary>
public static class IFParser
{
    private static bool _initialized = false;
    private static long _maxWordLen = 0;
    private static string DONT_UNDERSTAND_TEXT = "";
    private static List<ParserItem> _verbs = [];
    private static List<ParserItem> _nouns = [];
    private static List<ParserItem> _nounitems = [];
    private static List<ParserItem> _directions = [];
    private static List<ParserItem> _prepositions = [];
    private static List<ParserItem> _adjectives = [];
    private static List<ParserItem> _articles = []; // "a,an,the,..."

    /// <summary>
    /// Initializes parser data structures using values from the specified Grod instance.
    /// </summary>
    private static void ParseInit(Grod grod)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        _verbs = [.. grod.Items(VERB_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[VERB_PREFIX.Length..], SplitList(x.Value)))];
        _nouns = [.. grod.Items(NOUN_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[NOUN_PREFIX.Length..], SplitList(x.Value)))];
        _nounitems = [.. grod.Items(NOUNITEM_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[NOUNITEM_PREFIX.Length..], SplitList(x.Value)))];
        _directions = [.. grod.Items(DIRECTION_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[DIRECTION_PREFIX.Length..], SplitList(x.Value)))];
        _prepositions = [.. grod.Items(PREPOSITION_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[PREPOSITION_PREFIX.Length..], SplitList(x.Value)))];
        _adjectives = [.. grod.Items(ADJECTIVE_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new ParserItem(x.Key[ADJECTIVE_PREFIX.Length..], SplitList(x.Value)))];
        _articles = [.. SplitList(grod.Get(ARTICLE_KEY, true))
            .Select(x => new ParserItem(ARTICLE_KEY, SplitList(x)))];
        _maxWordLen = GetNumberValue(grod.Get(WORDSIZE, true));
        if (_maxWordLen > 0)
        {
            TrimSynonyms(ref _verbs);
            TrimSynonyms(ref _nouns);
            TrimSynonyms(ref _nounitems);
            TrimSynonyms(ref _directions);
            TrimSynonyms(ref _prepositions);
            TrimSynonyms(ref _adjectives);
            TrimSynonyms(ref _articles);
        }
        DONT_UNDERSTAND_TEXT = (grod.Get(DONT_UNDERSTAND, true) ??
            $"I don't understand \"{{0}}\".") + NL_CHAR;
    }

    /// <summary>
    /// Parses a user input string and generates a list of messages representing the interpreted command and its components.
    /// </summary>
    public static List<GrifMessage>? ParseInput(Grod grod, string input)
    {
        var result = new List<GrifMessage>();
        string? verb;
        string? verbWord;
        string? direction = null;
        string? directionWord = null;
        string? noun = null;
        string? nounWord = null;
        string? adjectiveList = null;
        string? preposition = null;
        string? prepositionWord = null;
        string? indirectNoun = null;
        string? indirectNounWord = null;
        string? indirectAdjectiveList = null;
        string? extraText = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        ParseInit(grod);
        var words = input.Replace(",", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (words.Count == 0)
        {
            return null;
        }
        // handle "west", "go west", "west go"
        (verb, verbWord) = GetMatchingWord(_verbs, ref words);
        if (words.Count > 0)
        {
            (direction, directionWord) = GetMatchingWord(_directions, ref words);
        }
        if (words.Count > 0)
        {
            (noun, nounWord, adjectiveList) = GetNoun(_nouns, ref words);
        }
        if (words.Count > 0)
        {
            (preposition, prepositionWord) = GetMatchingFirstWord(_prepositions, ref words);
            if (words.Count > 0)
            {
                (indirectNoun, indirectNounWord, indirectAdjectiveList) = GetNoun(_nouns, ref words);
            }
            else
            {
                result.Add(new GrifMessage(MessageType.Text, string.Format(DONT_UNDERSTAND_TEXT, input)));
                return result;
            }
        }
        if (verb == null && direction == null)
        {
            if (words.Count > 0 || noun != null)
            {
                verb = "?"; // any verb
                extraText = string.Join(' ', words);
                if (noun != null)
                {
                    extraText = $"{nounWord} {extraText}".Trim();
                    noun = null;
                    nounWord = null;
                }
                words.Clear();
            }
            else
            {
                result.Add(new GrifMessage(MessageType.Text, string.Format(DONT_UNDERSTAND_TEXT, input)));
                return result;
            }
        }
        string command;
        if (verb != null && direction != null)
        {
            command = $"{COMMAND_PREFIX}{verb}.{direction}";
        }
        else if (direction != null)
        {
            command = $"{COMMAND_PREFIX}{direction}";
            verb = direction;
            verbWord = directionWord;
            direction = null;
            directionWord = null;
        }
        else
        {
            command = $"{COMMAND_PREFIX}{verb}";
        }
        if (noun != null)
        {
            if (preposition != null && indirectNoun != null)
            {
                var tempCommand = $"{command}.{noun}.{preposition}.{indirectNoun}";
                if (grod.Get(tempCommand, true) != null)
                {
                    command = tempCommand;
                }
                else
                {
                    tempCommand = $"{command}.{noun}.{preposition}.*"; // any indirect noun
                    if (grod.Get(tempCommand, true) != null)
                    {
                        command = tempCommand;
                    }
                    else
                    {
                        command = $"{command}.*.{preposition}.*"; // any noun, any indirect noun
                    }
                }
            }
            else if (preposition != null) // no indirect noun
            {
                var tempCommand = $"{command}.{preposition}.{noun}";
                if (grod.Get(tempCommand, true) != null)
                {
                    command = tempCommand;
                }
                else
                {
                    command = $"{command}.{preposition}.*"; // any noun
                }
            }
            else if (grod.Get($"{command}.{noun}", true) != null)
            {
                command += $".{noun}";
            }
            else
            {
                command += ".*"; // any noun
            }
        }
        if (words.Count > 0)
        {
            if (grod.Get(command + ".?", true) != null)
            {
                command += ".?"; // any extra text
                extraText = string.Join(' ', words);
                words.Clear();
            }
            else
            {
                result.Add(new GrifMessage(MessageType.Text, string.Format(DONT_UNDERSTAND_TEXT, input)));
                return result;
            }
        }
        if (grod.Get(command, true) == null)
        {
            result.Add(new GrifMessage(MessageType.Text, string.Format(DONT_UNDERSTAND_TEXT, input)));
            return result;
        }
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.full,\"{input}\")"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.verb,{verb ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.verbword,{verbWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.direction,{direction ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.directionword,{directionWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.noun,{noun ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.nounword,{nounWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.nounadjectives,\"{adjectiveList ?? NULL}\")"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.preposition,{preposition ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.prepositionword,{prepositionWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.indirectnoun,{indirectNoun ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.indirectnounword,{indirectNounWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.indirectadjectives,\"{indirectAdjectiveList ?? NULL}\")"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.extratext,{extraText ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SCRIPT_TOKEN}{command})"));
        return result;
    }

    #region Private methods

    /// <summary>
    /// Trims synonyms in the provided list of ParserItem objects to the maximum word length.
    /// </summary>
    private static void TrimSynonyms(ref List<ParserItem> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var words = items[i].Values;
            for (int j = 0; j < words.Length; j++)
            {
                if (_maxWordLen > 0 && words[j].Length > _maxWordLen)
                {
                    words[j] = words[j][..(int)_maxWordLen];
                }
            }
            items[i] = new ParserItem(items[i].Key, words);
        }
    }

    /// <summary>
    /// Gets the first matching word from the provided vocabulary list and removes it from the words list.
    /// </summary>
    private static (string?, string?) GetMatchingWord(List<ParserItem> vocabList, ref List<string> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (_maxWordLen > 0 && word.Length > _maxWordLen)
            {
                word = word[..(int)_maxWordLen];
            }
            foreach (var item in vocabList)
            {
                var verbWords = item.Values;
                foreach (var syn in verbWords)
                {
                    if (word.Equals(syn, OIC))
                    {
                        word = words[i]; // use original string
                        words.RemoveAt(i);
                        return (item.Key, word);
                    }
                }
            }
        }
        return (null, null);
    }

    /// <summary>
    /// Gets the first matching word from the provided vocabulary list and removes it from the start of the words list.
    /// </summary>
    private static (string?, string?) GetMatchingFirstWord(List<ParserItem> vocabList, ref List<string> words)
    {
        if (words.Count == 0)
        {
            return (null, null);
        }
        var word = words[0];
        if (_maxWordLen > 0 && word.Length > _maxWordLen)
        {
            word = word[..(int)_maxWordLen];
        }
        foreach (var item in vocabList)
        {
            var verbWords = item.Values;
            foreach (var syn in verbWords)
            {
                if (word.Equals(syn, OIC))
                {
                    word = words[0]; // use original string
                    words.RemoveAt(0);
                    return (item.Key, word);
                }
            }
        }
        return (null, null);
    }

    /// <summary>
    /// Gets a noun from the provided noun list, along with any associated adjectives, and removes them from the words list.
    /// </summary>
    private static (string?, string?, string?) GetNoun(List<ParserItem> nounList, ref List<string> words)
    {
        // remove articles such as "the", "a", "an"
        RemoveArticles(_articles, ref words);
        // find any adjectives
        var adjectivesFound = new List<ParserItem>();
        string? adjectiveList = null;
        while (words.Count > 0)
        {
            var foundAdjective = false;
            foreach (var item in _adjectives)
            {
                if (item.Key.Equals(words[0], OIC))
                {
                    foundAdjective = true;
                    adjectivesFound.Add(item);
                    if (adjectiveList == null)
                    {
                        adjectiveList = item.Key;
                    }
                    else
                    {
                        adjectiveList += "," + item.Key;
                    }
                    words.RemoveAt(0);
                    break;
                }
            }
            if (!foundAdjective)
            {
                break;
            }
        }
        // find noun
        (var noun, var nounWord) = GetMatchingFirstWord(nounList, ref words);
        if (noun != null)
        {
            // verify that all adjectives apply to this noun
            var validAdjective = true;
            foreach (var adj in adjectivesFound)
            {
                var foundNoun = false;
                foreach (var tempNoun in adj.Values)
                {
                    if (noun.Equals(tempNoun, OIC))
                    {
                        foundNoun = true;
                        break;
                    }
                }
                if (!foundNoun)
                {
                    validAdjective = false;
                    break;
                }
            }
            if (validAdjective)
            {
                return (noun, nounWord, adjectiveList);
            }
        }
        // check for numeric noun
        if (words.Count > 0)
        {
            if (long.TryParse(words[0], out long number))
            {
                words.RemoveAt(0);
                return ("#", number.ToString(), adjectiveList); // numeric noun, normalized
            }
        }
        // no noun found
        return (null, null, null);
    }

    /// <summary>
    /// Removes articles from the start of the words list.
    /// </summary>
    private static void RemoveArticles(List<ParserItem> articles, ref List<string> words)
    {
        if (words.Count == 0)
        {
            return;
        }
        foreach (var item in articles)
        {
            foreach (var article in item.Values)
            {
                if (words.Count > 0 && words[0].Equals(article, OIC))
                {
                    words.RemoveAt(0);
                    break;
                }
            }
        }
    }

    #endregion
}
