using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Comparers
{
    // Basic and custom callbacks retain authored equal-beat order regardless of their type or payload.
    public sealed class EventOrderComparer : Comparer<BaseObject>
    {
        public static readonly EventOrderComparer Instance = new();

        // FileOrder belongs only to chronology. Clones and network packets match by content separately.
        public override int Compare(BaseObject left, BaseObject right)
        {
            var time = left.JsonTime.CompareTo(right.JsonTime);
            return time != 0
                ? time
                : left.FileOrder.CompareTo(right.FileOrder);
        }
    }
}
