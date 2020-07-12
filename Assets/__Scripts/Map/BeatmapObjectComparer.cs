using System.Collections.Generic;

public class BeatmapObjectComparer : IComparer<BeatmapObject>
{
    public int Compare(BeatmapObject x, BeatmapObject y)
    {
        if (x._time == y._time) return x.GetHashCode().CompareTo(y.GetHashCode());
        else return x._time.CompareTo(y._time);
    }
}
