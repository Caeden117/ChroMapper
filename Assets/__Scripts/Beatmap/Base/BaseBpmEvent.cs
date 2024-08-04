using System;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseBpmEvent : BaseEvent
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Bpm);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Bpm = reader.GetFloat();
            base.Deserialize(reader);
        }

        public BaseBpmEvent() {}

        public BaseBpmEvent(BaseBpmEvent other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Bpm = other.Bpm;
            CustomData = other.CustomData.Clone();
        }

        public BaseBpmEvent(float jsonTime, float bpm)
        {
            JsonTime = jsonTime;
            Bpm = bpm;
        }

        // Used for node editor
        public BaseBpmEvent(JSONNode node) : this(BeatmapFactory.BpmEvent(node)) {}

        public override int Type
        {
            get => 100;
            set {}
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.BpmChange;
        public float Bpm { get; set; }
        public int Beat { get; set; } = 0;

        // TODO Get rid of these unused custom keys by not inheriting BaseEvent
        #region   
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

        public override string CustomKeyColor { get; } = "unusedColor";
        public override string CustomKeyTrack { get; } = "unusedTrack";
        
        #endregion
        
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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2BpmEvent.ToJson(this),
            3 => V3BpmEvent.ToJson(this)
        };

        public override BaseItem Clone() {
            var bpm = new BaseBpmEvent(this);
            bpm.ParseCustom();
            return bpm;
        }
    }
}
