using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseLightRotationEventBoxGroup<T> : BaseEventBoxGroup<T> where T : BaseLightRotationEventBox
    {
        protected BaseLightRotationEventBoxGroup()
        {
        }

        protected BaseLightRotationEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }
    }
}
