using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Shared
{
    public class ObjectComparer : IComparer<BaseObject>
    {
        public int Compare(BaseObject x, BaseObject y)
        {
            return x.Time == y.Time ? x.GetHashCode().CompareTo(y.GetHashCode()) : x.Time.CompareTo(y.Time);
        }
    }
}
