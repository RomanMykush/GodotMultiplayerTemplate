using System.Collections.Generic;

namespace SteampunkDnD.Shared;

public static class DictionaryUtils
{
    public static void AppendItemToList<T, U>(this Dictionary<T, List<U>> dictionary, T key, U item) where T : notnull
    {
        if (!dictionary.ContainsKey(key))
            dictionary[key] = new();
        dictionary[key].Add(item);
    }
}
