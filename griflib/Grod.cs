using static GrifLib.Common;

namespace GrifLib;

/// <summary>
/// Initializes a new instance of the Grod class with the specified name, path, and parent.
/// </summary>
public class Grod(string? name = null, string? filePath = null, Grod? parent = null)
{
    #region private definitions

    // Lists of string representations for Boolean true and false values
    private readonly string[] _truthyList = [TRUE, "t", "yes", "y", "1", "-1"];
    private readonly string[] _falseyList = [FALSE, "f", "no", "n", "0", ""];

    // Internal storage for key-value pairs, using case-insensitive keys
    private readonly Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);

    #endregion

    /// <summary>
    /// Gets or sets the name associated with the object. Used to identify the Grod programmatically.
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// Path of the file from which this Grod was loaded.
    /// </summary>
    public string? FilePath { get; set; } = filePath;

    /// <summary>
    /// Gets or sets the parent Grod of this instance.
    /// </summary>
    public Grod? Parent { get; set; } = parent;

    /// <summary>
    /// Track if changes made to any item in this Grod.
    /// </summary>
    public bool Changed { get; set; } = false;

    /// <summary>
    /// Returns the number of keys in the collection, optionally including keys from nested collections.
    /// </summary>
    public int Count(bool recursive)
    {
        var keys = Keys(recursive, false);
        return keys.Count;
    }

    /// <summary>
    /// Retrieves the value associated with the specified key, optionally searching parent collections recursively.
    /// </summary>
    public string? Get(string key, bool recursive)
    {
        ValidateKey(key);
        if (_data.TryGetValue(key, out var value))
        {
            if (value == null || value.Equals(NULL, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return value;
        }
        if (recursive && Parent != null)
        {
            return Parent.Get(key, recursive);
        }
        return null;
    }

    /// <summary>
    /// Retrieves the numeric value associated with the specified key, optionally searching parent collections recursively.
    /// </summary>
    public long? GetNumber(string key, bool recursive)
    {
        var value = Get(key, recursive);
        if (value == null)
        {
            return null;
        }
        if (long.TryParse(value, out long longValue))
        {
            return longValue;
        }
        throw new FormatException($"Value for key '{key}' is not a valid number.");
    }

    /// <summary>
    /// Retrieves the value associated with the specified key and attempts to convert it to a Boolean value.
    /// </summary>
    public bool? GetBool(string key, bool recursive)
    {
        var value = Get(key, recursive);
        if (value == null)
        {
            return null;
        }
        if (_truthyList.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }
        if (_falseyList.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        if (bool.TryParse(value, out bool boolValue))
        {
            return boolValue;
        }
        throw new FormatException($"Value for key '{key}' is not a valid boolean.");
    }

    /// <summary>
    /// Adds a new entry or updates the value associated with the specified key.
    /// </summary>
    public void Set(string key, string? value)
    {
        ValidateKey(key);
        if (value != null && value.Equals(NULL, StringComparison.OrdinalIgnoreCase))
        {
            value = null;
        }
        if (!_data.TryAdd(key, value))
        {
            _data[key] = value;
        }
        Changed = true;
    }

    /// <summary>
    /// Sets the value associated with the specified key, storing the value as a 64-bit integer.
    /// </summary>
    public void Set(string key, long value)
    {
        Set(key, value.ToString());
    }

    /// <summary>
    /// Sets the value associated with the specified key to the given Boolean value.
    /// </summary>
    public void Set(string key, bool value)
    {
        Set(key, value ? TRUE : FALSE);
    }

    /// <summary>
    /// Sets the value associated with the specified item key in the collection.
    /// </summary>
    public void Set(GrodItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Set(item.Key, item.Value);
    }

    /// <summary>
    /// Removes the entry with the specified key from the current collection. Optionally removes the entry from parent
    /// collections if recursive removal is requested.
    /// </summary>
    public void Remove(string key, bool recursive)
    {
        ValidateKey(key);
        _data.Remove(key);
        Changed = true;
        if (recursive && Parent != null)
        {
            Parent.Remove(key, recursive);
        }
    }

    /// <summary>
    /// Removes all items from the current collection, and optionally clears items from parent collections recursively.
    /// </summary>
    public void Clear(bool recursive)
    {
        _data.Clear();
        Changed = false;
        if (recursive && Parent != null)
        {
            Parent.Clear(recursive);
        }
    }

    /// <summary>
    /// Determines whether the collection contains the specified key, optionally searching parent collections
    /// recursively.
    /// </summary>
    public bool ContainsKey(string key, bool recursive)
    {
        ValidateKey(key);
        if (_data.ContainsKey(key))
        {
            return true;
        }
        if (recursive && Parent != null)
        {
            return Parent.ContainsKey(key, recursive);
        }
        return false;
    }

    /// <summary>
    /// Retrieves a list of all keys in the collection, optionally including keys from parent collections recursively and optionally sorted.
    /// </summary>
    public List<string> Keys(bool recursive, bool sorted)
    {
        var keys = new List<string>(_data.Keys);
        if (recursive && Parent != null)
        {
            var parentKeys = Parent.Keys(recursive, false);
            keys = [.. keys.Union(parentKeys, StringComparer.OrdinalIgnoreCase)];
        }
        if (sorted)
        {
            keys.Sort(CompareKeys);
        }
        return keys;
    }

    /// <summary>
    /// Retrieves a list of keys that begin with the specified prefix.
    /// </summary>
    public List<string> Keys(string prefix, bool recursive, bool sorted)
    {
        var keys = new List<string>(_data.Keys)
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (recursive && Parent != null)
        {
            var parentKeys = Parent.Keys(recursive, false)
                .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            keys = [.. keys.Union(parentKeys, StringComparer.OrdinalIgnoreCase)];
        }
        if (sorted)
        {
            keys.Sort(CompareKeys);
        }
        return keys;
    }

    /// <summary>
    /// Returns a list of all items in the collection, with options to include nested items and to sort the results.
    /// </summary>
    public List<GrodItem> Items(bool recursive, bool sorted)
    {
        var keys = Keys(recursive, sorted);
        List<GrodItem> items = [];
        foreach (var key in keys)
        {
            var value = Get(key, recursive);
            items.Add(new GrodItem(key, value));
        }
        return items;
    }

    /// <summary>
    /// Retrieves a list of items whose keys match the specified prefix, with options for recursion and sorting.
    /// </summary>
    public List<GrodItem> Items(string prefix, bool recursive, bool sorted)
    {
        var keys = Keys(prefix, recursive, sorted);
        List<GrodItem> items = [];
        foreach (var key in keys)
        {
            var value = Get(key, recursive);
            items.Add(new GrodItem(key, value));
        }
        return items;
    }

    /// <summary>
    /// Adds the specified collection of items to the current set.
    /// </summary>
    public void AddItems(IEnumerable<GrodItem> items)
    {
        if (items != null)
        {
            foreach (var item in items)
            {
                Set(item);
            }
        }
    }

    /// <summary>
    /// Attempts to determine whether the specified key is valid.
    /// </summary>
    public static bool TryValidateKey(string key)
    {
        try
        {
            ValidateKey(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the parent Grod with the specified name, searching recursively up the parent chain. Returns null if no matching parent is found.
    /// </summary>
    public Grod? GetGrod(string name)
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent.Name != null && parent.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }
            parent = parent.Parent;
        }
        return null;
    }

    #region private methods

    /// <summary>
    /// Compares two dot-delimited key strings using a custom ordering that accounts for special wildcard tokens and
    /// numeric values.
    /// </summary>
    public static int CompareKeys(string x, string y)
    {
        if (x == null)
        {
            if (y == null) return 0;
            return -1;
        }
        if (y == null)
        {
            return 1;
        }
        if (x.Equals(y, StringComparison.OrdinalIgnoreCase)) return 0;
        var xTokens = x.Split('.');
        var yTokens = y.Split('.');
        for (int i = 0; i < Math.Max(xTokens.Length, yTokens.Length); i++)
        {
            if (i >= xTokens.Length) return -1; // x is shorter and earlier
            if (i >= yTokens.Length) return 1; // y is shorter and earlier
            if (xTokens[i].Equals(yTokens[i], StringComparison.OrdinalIgnoreCase)) continue;
            if (xTokens[i] == "*") return -1; // "*" comes first so x is earlier
            if (yTokens[i] == "*") return 1; // "*" comes first so y is earlier
            if (xTokens[i] == "?") return -1; // "?" comes next so x is earlier
            if (yTokens[i] == "?") return 1; // "?" comes next so y is earlier
            if (xTokens[i] == "#") return -1; // "#" comes next so x is earlier
            if (yTokens[i] == "#") return 1; // "#" comes next so y is earlier
            if (long.TryParse(xTokens[i], out long xVal) && long.TryParse(yTokens[i], out long yVal))
            {
                if (xVal == yVal) continue;
                return xVal < yVal ? -1 : 1;
            }
            return string.Compare(xTokens[i], yTokens[i], StringComparison.OrdinalIgnoreCase);
        }
        return 0;
    }

    /// <summary>
    /// Validates that the specified key does not contain invalid characters or whitespace.
    /// Only special characters used by the DAGS language are excluded. All others are valid.
    /// </summary>
    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null, empty, or only whitespace.", nameof(key));
        }
        var isScriptKey = key[0] == SCRIPT_CHAR;
        for (int i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (char.IsControl(c))
            {
                throw new ArgumentException("Key cannot contain control characters.", nameof(key));
            }
            if (char.IsWhiteSpace(c))
            {
                throw new ArgumentException("Key cannot contain whitespace characters.", nameof(key));
            }
            if (c == PARAM_CHAR || c == '"' || c == '\\')
            {
                throw new ArgumentException($"Key cannot contain special characters: {c}", nameof(key));
            }
            if (!isScriptKey && (c == '(' || c == ')' || c == ','))
            {
                throw new ArgumentException($"Key cannot contain punctuation characters: {c}", nameof(key));
            }
            if (i != 0 && c == SCRIPT_CHAR)
            {
                throw new ArgumentException($"Key cannot contain '{SCRIPT_CHAR}' except at the start.", nameof(key));
            }
        }
    }

    #endregion
}
