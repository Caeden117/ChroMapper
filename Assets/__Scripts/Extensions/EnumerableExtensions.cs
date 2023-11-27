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

    // TODO(Caeden): Look into replacing with BinarySearch from System.MemoryExtensions https://learn.microsoft.com/en-us/dotnet/api/system.memoryextensions.binarysearch
    // TODO(Caeden): Scratch that, its not available in .NET Standard 2.1.
    //   Instead, consider https://github.com/atcarter714/UnityH4xx which uses manual IL to create a span off of a List's backing array (only safe for read operations)
    public static int BinarySearchBy<TValue, TComparison>(this IList<TValue> list, TComparison value, Func<TValue, TComparison> getter) where TComparison : IComparable<TComparison>
    {
        var min = 0;
        var max = list.Count - 1;
        var mid = 0;

        while (min <= max)
        {
            mid = (min + max) / 2;

            var otherValue = getter(list[mid]);

            switch (value.CompareTo(otherValue))
            {
                case 0:
                    return mid;
                case >= 1:
                    min = mid + 1;
                    break;
                case <= -1:
                    max = mid - 1;
                    break;
            }
        }

        return ~mid;
    }
}
