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

    public static int BinarySearchBy<TValue, TComparison>(
        this IReadOnlyList<TValue> list,
        TComparison value,
        Func<TValue, TComparison> getter)
        where TComparison : IComparable<TComparison>
    {
        if (list is List<TValue> concreteList)
        {
            return BinarySearchBy(concreteList.AsSpan(), value, getter);
        }

        if (list is TValue[] array)
        {
            ReadOnlySpan<TValue> span = array;
            return BinarySearchBy(span, value, getter);
        }

        var min = 0;
        var max = list.Count - 1;
        while (min <= max)
        {
            var mid = min + ((max - min) / 2);
            var comparison = value.CompareTo(getter(list[mid]));
            if (comparison == 0)
            {
                return mid;
            }

            if (comparison > 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid - 1;
            }
        }

        return ~min;
    }

    // Mutable spans share the read-only implementation so array covariance is never exposed to write access.
    public static int BinarySearchBy<TValue, TComparison>(
        this Span<TValue> span,
        TComparison value,
        Func<TValue, TComparison> getter)
        where TComparison : IComparable<TComparison> =>
        BinarySearchBy((ReadOnlySpan<TValue>)span, value, getter);

    public static int BinarySearchBy<TValue, TComparison>(
        this ReadOnlySpan<TValue> span,
        TComparison value,
        Func<TValue, TComparison> getter)
        where TComparison : IComparable<TComparison>
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

        // Return the complement of the true insertion point so range callers do not retain a neighboring boundary item.
        return ~min;
    }

    // ContainerCollectionTest.GetBetween requires the first stacked item at an inclusive lower range boundary.
    public static int LowerBoundBy<TValue, TComparison>(
        this Span<TValue> span,
        TComparison value,
        Func<TValue, TComparison> getter)
        where TComparison : IComparable<TComparison>
    {
        var min = 0;
        var max = span.Length;

        while (min < max)
        {
            var mid = min + ((max - min) / 2);
            if (getter(span[mid]).CompareTo(value) < 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid;
            }
        }

        return min;
    }

    // ContainerCollectionTest.GetBetween requires the position after the final stacked item at an inclusive upper boundary.
    public static int UpperBoundBy<TValue, TComparison>(
        this Span<TValue> span,
        TComparison value,
        Func<TValue, TComparison> getter)
        where TComparison : IComparable<TComparison>
    {
        var min = 0;
        var max = span.Length;

        while (min < max)
        {
            var mid = min + ((max - min) / 2);
            if (getter(span[mid]).CompareTo(value) <= 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid;
            }
        }

        return min;
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
