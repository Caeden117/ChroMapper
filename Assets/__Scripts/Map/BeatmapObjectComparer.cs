using System.Collections.Generic;

public class BeatmapObjectComparer : IComparer<BeatmapObject>
{
    public int Compare(BeatmapObject x, BeatmapObject y)
    {
        if (x.Time == y.Time) return x.GetHashCode().CompareTo(y.GetHashCode());
        return x.Time.CompareTo(y.Time);
    }
}
