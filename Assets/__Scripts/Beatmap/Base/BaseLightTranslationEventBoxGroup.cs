using System.Collections.Generic;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationEventBoxGroup<T> : BaseEventBoxGroup<T>
        where T : BaseLightTranslationEventBox
    {
        public BaseLightTranslationEventBoxGroup()
        {
        }

        protected BaseLightTranslationEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }

        public override string CustomKeyColor { get; }
        public override string CustomKeyTrack { get; }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightTranslationEventBoxGroup.ToJson(this)
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
