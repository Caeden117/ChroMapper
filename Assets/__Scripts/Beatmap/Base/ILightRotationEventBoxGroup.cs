using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class ILightRotationEventBoxGroup<T> : IEventBoxGroup<T> where T : ILightRotationEventBox
    {
        protected ILightRotationEventBoxGroup()
        {
        }

        protected ILightRotationEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }
    }
}
