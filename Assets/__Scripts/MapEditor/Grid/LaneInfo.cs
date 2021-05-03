using System;

public class LaneInfo : IComparable
{
    public int Type { get; private set; }
    private int sortOrder;
    public string Name;

    public LaneInfo(int i, int v)
    {
        sortOrder = v;
        Type = i;
    }

    public int CompareTo(object obj)
    {
        if (obj is LaneInfo other) {
            return sortOrder - other.sortOrder;
        }
        return 0;
    }
}
