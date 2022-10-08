using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class IBpmEvent : IObject
    {
        protected IBpmEvent()
        {
        }

        protected IBpmEvent(IBpmEvent other)
        {
            Time = other.Time;
            Bpm = other.Bpm;
            CustomData = other.CustomData?.Clone();
        }

        protected IBpmEvent(IEvent evt)
        {
            Time = evt.Time;
            Bpm = evt.FloatValue;
            CustomData = evt.CustomData?.Clone();
        }

        protected IBpmEvent(float time, float bpm, JSONNode customData = null) : base(time, customData) => Bpm = bpm;

        public override ObjectType ObjectType { get; set; } = ObjectType.BpmChange;
        public float Bpm { get; set; }
        public int Beat { get; set; } = 0;

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is IBpmEvent bpm) return Math.Abs(Bpm - bpm.Bpm) < DecimalTolerance;
            return false;
        }

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IBpmEvent bpm) Bpm = bpm.Bpm;
        }
    }
}
