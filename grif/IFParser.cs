using static Grif.Common;
using static Grif.Dags;

namespace Grif;

/*
Can handle the following input patterns:
    verb
    direction
    verb direction
    verb noun
    verb noun preposition indirectnoun
    verb preposition noun
The order of the words does not matter, so "go west" and "west go" are equivalent.
Noun must come before indirect noun if both are present.
*/

public static class IFParser
{
    private static long _maxWordLen = 0;
    private static string DONT_UNDERSTAND_TEXT = "";
    private static List<GrodItem> _verbs = [];
    private static List<GrodItem> _nouns = [];
    private static List<GrodItem> _nounitems = [];
    private static List<GrodItem> _directions = [];
    private static List<GrodItem> _prepositions = [];
    private static List<GrodItem> _adjectives = [];
    private static List<GrodItem> _articles = []; // "the,a,an,some,any,my,his,her,its,our,their"

    public static void ParseInit(Grod grod)
    {
        _verbs = [.. grod.Items(VERB_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[VERB_PREFIX.Length..], x.Value))];
        _nouns = [.. grod.Items(NOUN_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[NOUN_PREFIX.Length..], x.Value))];
        _nounitems = [.. grod.Items(NOUNITEM_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[NOUNITEM_PREFIX.Length..], x.Value))];
        _directions = [.. grod.Items(DIRECTION_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[DIRECTION_PREFIX.Length..], x.Value))];
        _prepositions = [.. grod.Items(PREPOSITION_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[PREPOSITION_PREFIX.Length..], x.Value))];
        _adjectives = [.. grod.Items(ADJECTIVE_PREFIX, true, true)
            .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != NULL)
            .Select(x => new GrodItem(x.Key[ADJECTIVE_PREFIX.Length..], x.Value))];
        _articles = grod.Items(ARTICLE_KEY, true, true);
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

    public static List<GrifMessage>? ParseInput(Grod grod, string input)
    {
        var result = new List<GrifMessage>();
        string? verb;
        string? verbWord;
        string? direction = null;
        string? directionWord = null;
        string? noun = null;
        string? nounWord = null;
        string? preposition = null;
        string? prepositionWord = null;
        string? indirectNoun = null;
        string? indirectNounWord = null;
        string? extraText = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (words.Count == 0)
        {
            return null;
        }
        RemoveArticles(_articles, ref words);
        // handle "west", "go west", "west go"
        (verb, verbWord) = GetMatchingWord(_verbs, ref words);
        if (words.Count > 0)
        {
            (direction, directionWord) = GetMatchingWord(_directions, ref words);
        }
        if (words.Count > 0)
        {
            (noun, nounWord) = GetNoun(_nouns, ref words);
        }
        if (words.Count > 0)
        {
            (preposition, prepositionWord) = GetMatchingWord(_prepositions, ref words);
            if (words.Count > 0)
            {
                (indirectNoun, indirectNounWord) = GetNoun(_nouns, ref words);
            }
            else
            {
                result.Add(new GrifMessage(MessageType.Text, string.Format(DONT_UNDERSTAND_TEXT, input)));
                return result;
            }
        }
        if (verb == null && direction == null)
        {
            if (words.Count > 0)
            {
                verb = "?"; // any verb
                extraText = string.Join(' ', words);
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
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.preposition,{preposition ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.prepositionword,{prepositionWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.indirectnoun,{indirectNoun ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.indirectnounword,{indirectNounWord ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SET_TOKEN}input.extratext,{extraText ?? NULL})"));
        result.Add(new GrifMessage(MessageType.Script, $"{SCRIPT_TOKEN}{command})"));
        return result;
    }

    #region Private methods

    private static void TrimSynonyms(ref List<GrodItem> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var words = items[i].Value!.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < words.Length; j++)
            {
                if (_maxWordLen > 0 && words[j].Length > _maxWordLen)
                {
                    words[j] = words[j][..(int)_maxWordLen];
                }
            }
            items[i] = new GrodItem(items[i].Key, string.Join(',', words));
        }
    }

    private static (string?, string?) GetMatchingWord(List<GrodItem> vocabList, ref List<string> words)
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
                var verbWords = item.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries);
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

    private static (string?, string?) GetNoun(List<GrodItem> nounList, ref List<string> words)
    {
        (var noun, var nounWord) = GetMatchingWord(nounList, ref words);
        if (noun != null)
        {
            return (noun, nounWord);
        }
        for (int i = 0; i < words.Count; i++)
        {
            if (long.TryParse(words[i], out long number))
            {
                words.RemoveAt(i);
                return ("#", number.ToString()); // numeric noun, normalized
            }
        }
        return (null, null);
    }

    private static void RemoveArticles(List<GrodItem> articles, ref List<string> words)
    {
        for (int i = words.Count - 1; i >= 0; i--)
        {
            foreach (var item in articles)
            {
                var articleWords = item.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var syn in articleWords)
                {
                    if (words[i].Equals(syn, OIC))
                    {
                        words.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    #endregion
}
