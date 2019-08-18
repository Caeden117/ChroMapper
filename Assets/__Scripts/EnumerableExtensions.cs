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

    public static IList<int> AllIndexOf(this string text, string str, bool standardizeUpperCase = true,
        StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        IList<int> allIndexOf = new List<int>();
        string newSource = standardizeUpperCase ? text.ToUpper() : text;
        string newStr = standardizeUpperCase ? str.ToUpper() : str;
        int index = newSource.IndexOf(newStr, comparisonType);
        while (index != -1)
        {
            allIndexOf.Add(index);
            index = newSource.IndexOf(newStr, index + newStr.Length, comparisonType);
        }
        return allIndexOf;
    }
}
