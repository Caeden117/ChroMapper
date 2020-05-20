using System.Collections.Generic;

public class BeatmapObjectComparer : IComparer<BeatmapObject>
{
    public int Compare(BeatmapObject x, BeatmapObject y)
    {
        if (x._time >= y._time) return 1;
        else return -1;
    }
}
