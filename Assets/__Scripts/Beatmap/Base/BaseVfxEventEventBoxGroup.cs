using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseVfxEventEventBoxGroup<T> : BaseEventBoxGroup<T>
        where T : BaseVfxEventEventBox
    {
        public int Type { get; set; }

        protected BaseVfxEventEventBoxGroup()
        {
        }

        protected BaseVfxEventEventBoxGroup(float time, int id, int type, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
            Type = type;
        }
    }
}
