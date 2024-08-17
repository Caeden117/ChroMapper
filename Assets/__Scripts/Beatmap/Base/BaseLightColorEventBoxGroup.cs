using System.Collections.Generic;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorEventBoxGroup<T> : BaseEventBoxGroup<T> where T : BaseLightColorEventBox
    {
        public BaseLightColorEventBoxGroup()
        {
        }

        protected BaseLightColorEventBoxGroup(float time, int id, List<T> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightColorEventBoxGroup.ToJson(this),
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();


        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";
    }
}
