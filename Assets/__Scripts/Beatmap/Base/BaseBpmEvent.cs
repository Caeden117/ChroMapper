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
            JsonTime = other.JsonTime;
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

        public override string CustomKeyColor { get; } = "unusedColor";
        public override string CustomKeyTrack { get; } = "unusedTrack";
        
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

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseBpmEvent bpmEvent) return comparison;

            // Compare by BPM
            if (comparison == 0) comparison = Bpm.CompareTo(bpmEvent.Bpm);

            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2BpmEvent.ToJson(this),
            3 or 4 => V3BpmEvent.ToJson(this)
        };

        public override BaseItem Clone() {
            var bpm = new BaseBpmEvent(this);
            bpm.ParseCustom();
            return bpm;
        }
    }
}
