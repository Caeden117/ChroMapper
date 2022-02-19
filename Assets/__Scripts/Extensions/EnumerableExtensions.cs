using System;
using System.Collections.Generic;

public static class IEnumerableExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
                yield return element;
        }
    }

    public static IList<int> AllIndexOf(this string text, string str, bool standardizeUpperCase = true,
        StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        IList<int> allIndexOf = new List<int>();
        var newSource = standardizeUpperCase ? text.ToUpper() : text;
        var newStr = standardizeUpperCase ? str.ToUpper() : str;
        var index = newSource.IndexOf(newStr, comparisonType);
        while (index != -1)
        {
            allIndexOf.Add(index);
            index = newSource.IndexOf(newStr, index + newStr.Length, comparisonType);
        }

        return allIndexOf;
    }
}
