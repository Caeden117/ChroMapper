using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    // FloatFX data is mutable and keys active-container dictionaries, so it must retain reference equality across lanes.
    public class BaseFxEventFloat : BaseFxEvent<float>
    {
        public BaseFxEventFloat()
        {
        }

        // Used for Node Editor
        public BaseFxEventFloat(JSONNode node) : this(V3FloatFxEvent.GetFromJson(node))
        {
        }

        protected BaseFxEventFloat(
            float time,
            float value,
            int easing,
            int usePrevious,
            JSONNode customData = null) : base(time, value, customData)
        {
            Value = value;
            Easing = easing;
            UsePrevious = usePrevious;
        }

        protected BaseFxEventFloat(BaseFxEventFloat other) : base(other)
        {
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3FloatFxEvent.ToJson(this)
            };

        public override BaseItem Clone() => new BaseFxEventFloat(this);

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseFxEventFloat fx) return BoxIndex == fx.BoxIndex;
            return false;
        }
    }
}
