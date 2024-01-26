using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Comparers
{
    public class BaseObjectComparer : Comparer<BaseObject>
    {
        public static readonly BaseObjectComparer Comparer = new();

        public override int Compare(BaseObject a, BaseObject b) => a.JsonTime.CompareTo(b.JsonTime); 
    }
}
