using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Shared
{
    public class ObjectComparer : IComparer<BaseObject>
    {
        public int Compare(BaseObject x, BaseObject y) =>
            x.JsonTime == y.JsonTime ? x.GetHashCode().CompareTo(y.GetHashCode()) : x.JsonTime.CompareTo(y.JsonTime);
    }
}
