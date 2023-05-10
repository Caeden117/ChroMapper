using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseBpmEvent : BaseEvent
    {
        protected BaseBpmEvent() => Type = 100;

        protected BaseBpmEvent(BaseBpmEvent other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Bpm = other.Bpm;
            Type = 100;
            Value = 0;
            FloatValue = other.Bpm;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseBpmEvent(BaseEvent evt)
        {
            SetTimes(evt.JsonTime, evt.SongBpmTime);
            Bpm = evt.FloatValue;
            Type = 100;
            Value = 0;
            FloatValue = evt.FloatValue;
            CustomData = evt.SaveCustom().Clone();
        }

        protected BaseBpmEvent(float time, float bpm, JSONNode customData = null) :
            base(time, 100, 0, bpm, customData) => Bpm = bpm;

        public override ObjectType ObjectType { get; set; } = ObjectType.BpmChange;
        public float Bpm { get; set; }
        public int Beat { get; set; } = 0;

        public override string CustomKeyPropID { get; } = "unusedPropID";

        public override string CustomKeyLightID { get; } = "unusedLightID";

        public override string CustomKeyLerpType { get; } = "unusedLerpType";

        public override string CustomKeyEasing { get; } = "unusedEasing";

        public override string CustomKeyLightGradient { get; } = "unusedLightGradient";

        public override string CustomKeyStep { get; } = "unusedStep";

        public override string CustomKeyProp { get; } = "unusedProp";

        public override string CustomKeySpeed { get; } = "unusedSpeed";

        public override string CustomKeyRingRotation { get; } = "unusedRotation";

        public override string CustomKeyStepMult { get; } = "unusedStepMult";

        public override string CustomKeyPropMult { get; } = "unusedPropMult";

        public override string CustomKeySpeedMult { get; } = "unusedSpeedMult";

        public override string CustomKeyPreciseSpeed { get; } = "unusedPreciseSpeed";

        public override string CustomKeyDirection { get; } = "unusedDirection";

        public override string CustomKeyLockRotation { get; } = "unusedLockRotation";

        public override string CustomKeyLaneRotation { get; } = "unusedRotation";

        public override string CustomKeyNameFilter { get; } = "unusedNameFilter";

        public override bool IsBpmEvent() => true;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            return (other is BaseBpmEvent bpm);
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseBpmEvent bpm) Bpm = bpm.Bpm;
        }
    }
}
