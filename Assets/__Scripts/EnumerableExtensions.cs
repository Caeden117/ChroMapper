using System;
using System.Collections.Generic;

static class IEnumerableExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        HashSet<TKey> seenKeys = new HashSet<TKey>();
        foreach (TSource element in source)
            if (seenKeys.Add(keySelector(element))) yield return element;
    }

    public static IList<int> AllIndexOf(this string text, string str, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        IList<int> allIndexOf = new List<int>();
        int index = text.IndexOf(str, comparisonType);
        while (index != -1)
        {
            allIndexOf.Add(index);
            index = text.IndexOf(str, index + str.Length, comparisonType);
        }
        return allIndexOf;
    }
}
