using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearchBy<TValue, TComparison>(this List<TValue> list, TComparison value, Func<TValue, TComparison> getter) where TComparison : IComparable<TComparison>
    {
        var span = list.AsSpan();

        return BinarySearchBy(span, value, getter);
    }

    public static int BinarySearchBy<TValue, TComparison>(this Span<TValue> span, TComparison value, Func<TValue, TComparison> getter) where TComparison : IComparable<TComparison>
    {
        var min = 0;
        var max = span.Length - 1;
        var mid = 0;

        while (min <= max)
        {
            mid = (min + max) / 2;

            var otherValue = getter(span[mid]);

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

    public static int CountNoAlloc<T>(this List<T> list, Func<T, bool> predicate)
    {
        var span = list.AsSpan();
        var count = 0;
        var length = span.Length;

        for (var i = 0; i < length; i++)
        {
            if (predicate(span[i])) count++;
        }

        return count;
    }
}
