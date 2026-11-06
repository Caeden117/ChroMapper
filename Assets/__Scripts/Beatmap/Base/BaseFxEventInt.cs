using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseFxEventInt : BaseFxEvent<int>
    {
        public BaseFxEventInt()
        {
        }

        protected BaseFxEventInt(
            float time,
            int value,
            int usePrevious,
            JSONNode customData = null) : base(time, value, customData)
        {
            Value = value;
            UsePrevious = usePrevious;
        }

        protected BaseFxEventInt(BaseFxEventInt other) : base(other) { }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3IntFxEvent.ToJson(this)
            };

        public override BaseItem Clone() => new BaseFxEventInt(this);

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) =>
            GetHashCode() == other.GetHashCode();
    }
}
