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
            => CompareKeys(left.JsonTime, left.FileOrder, right.JsonTime, right.FileOrder);

        internal static int CompareKeys(float leftTime, int leftOrder, float rightTime, int rightOrder)
        {
            var time = leftTime.CompareTo(rightTime);
            return time != 0
                ? time
                : leftOrder.CompareTo(rightOrder);
        }
    }
}
