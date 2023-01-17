using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseLightTranslationEventBoxGroup<T> : BaseEventBoxGroup<T>
        where T : BaseLightTranslationEventBox
    {
        protected BaseLightTranslationEventBoxGroup()
        {
        }

        protected BaseLightTranslationEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }
    }
}
