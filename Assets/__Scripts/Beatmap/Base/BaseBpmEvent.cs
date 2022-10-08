using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseBpmEvent : BaseObject
    {
        protected BaseBpmEvent()
        {
        }

        protected BaseBpmEvent(BaseBpmEvent other)
        {
            Time = other.Time;
            Bpm = other.Bpm;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseBpmEvent(BaseEvent evt)
        {
            Time = evt.Time;
            Bpm = evt.FloatValue;
            CustomData = evt.CustomData?.Clone();
        }

        protected BaseBpmEvent(float time, float bpm, JSONNode customData = null) : base(time, customData) => Bpm = bpm;

        public override ObjectType ObjectType { get; set; } = ObjectType.BpmChange;
        public float Bpm { get; set; }
        public int Beat { get; set; } = 0;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseBpmEvent bpm) return Math.Abs(Bpm - bpm.Bpm) < DecimalTolerance;
            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseBpmEvent bpm) Bpm = bpm.Bpm;
        }
    }
}
