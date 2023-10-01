using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseLightColorEventBoxGroup<T> : BaseEventBoxGroup<T> where T : BaseLightColorEventBox
    {
        protected BaseLightColorEventBoxGroup()
        {
        }

        protected BaseLightColorEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }
    }
}
