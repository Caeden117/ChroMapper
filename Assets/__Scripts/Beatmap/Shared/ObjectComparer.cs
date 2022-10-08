using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Shared
{
    public class ObjectComparer : IComparer<IObject>
    {
        public int Compare(IObject x, IObject y)
        {
            return x.Time == y.Time ? x.GetHashCode().CompareTo(y.GetHashCode()) : x.Time.CompareTo(y.Time);
        }
    }
}
