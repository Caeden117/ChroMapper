using System.Collections.Generic;

public static class ListExtensions
{
    public static void RemoveSwapBack<T>(this IList<T> list, T item)
    {
        var index = list.IndexOf(item);
        if (index < 0) return;
        list.RemoveAtSwapBack(list.IndexOf(item));
    }

    public static void RemoveAtSwapBack<T>(this IList<T> list, int index)
    {
        if (index != list.Count - 1) list[index] = list[^1];
        list.RemoveAt(list.Count - 1);
    }
}
