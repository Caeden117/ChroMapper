using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class IColorBoostEvent : IObject
    {
        protected IColorBoostEvent()
        {
        }

        protected IColorBoostEvent(IColorBoostEvent other)
        {
            Time = other.Time;
            Toggle = other.Toggle;
            CustomData = other.CustomData?.Clone();
        }

        protected IColorBoostEvent(IEvent evt)
        {
            Time = evt.Time;
            Toggle = evt.Value == 1;
            CustomData = evt.CustomData?.Clone();
        }

        protected IColorBoostEvent(float time, bool toggle, JSONNode customData = null) : base(time, customData) => Toggle = toggle;

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public bool Toggle { get; set; }

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is IColorBoostEvent cbe) return Toggle = cbe.Toggle;
            return false;
        }

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IColorBoostEvent boost) Toggle = boost.Toggle;
        }
    }
}
