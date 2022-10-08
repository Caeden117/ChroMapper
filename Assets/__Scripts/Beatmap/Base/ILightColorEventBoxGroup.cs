using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class ILightColorEventBoxGroup<T> : IEventBoxGroup<T> where T : ILightColorEventBox
    {
        protected ILightColorEventBoxGroup()
        {
        }

        protected ILightColorEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }
    }
}
