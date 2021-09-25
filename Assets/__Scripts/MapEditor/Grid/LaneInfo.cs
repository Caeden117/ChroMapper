using System;

public class LaneInfo : IComparable
{
    private readonly int sortOrder;
    public string Name;

    public LaneInfo(int i, int v)
    {
        sortOrder = v;
        Type = i;
    }

    public int Type { get; }

    public int CompareTo(object obj)
    {
        if (obj is LaneInfo other) return sortOrder - other.sortOrder;
        return 0;
    }
}
