using System;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseRotationEvent : BaseObject
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Type);
            writer.Put((int)ExecutionTime);
            writer.Put(Rotation);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Type = reader.GetInt();
            ExecutionTime = (ExecutionTime)reader.GetInt();
            Rotation = reader.GetFloat();
            base.Deserialize(reader);
        }

        public BaseRotationEvent()
        {
        }

        public BaseRotationEvent(BaseEvent other)
        {
            JsonTime = other.JsonTime;
            Type = other.Type;
            Value = other.Value;
            CustomData = other.SaveCustom().Clone();
        }

        public BaseRotationEvent(BaseRotationEvent other)
        {
            JsonTime = other.JsonTime;
            ExecutionTime = other.ExecutionTime;
            Rotation = other.Rotation;
            CustomData = other.SaveCustom().Clone();
        }

        // Used for Node Editor
        public BaseRotationEvent(JSONNode node) : this(BeatmapFactory.RotationEvent(node)) { }

        public override ObjectType ObjectType { get; set; } = ObjectType.RotationEvent;
        public ExecutionTime ExecutionTime { get; set; }
        public float Rotation { get; set; }

        public int Type
        {
            get =>
                ExecutionTime switch
                {
                    ExecutionTime.Late => (int)EventTypeValue.LateRotationEventType,
                    _ => (int)EventTypeValue.EarlyRotationEventType
                };
            set =>
                ExecutionTime = value switch
                {
                    (int)EventTypeValue.LateRotationEventType => ExecutionTime.Late,
                    _ => ExecutionTime.Early
                };
        }

        public int Value
        {
            get
            {
                if (Rotation is >= -60 and <= -15 or >= 15 and <= 60 && Rotation % 15 == 0)
                    return Array.IndexOf(lightValueToRotationDegrees, (int)Rotation);
                return 1360 + (int)(Rotation % 360);
            }
            set
            {
                if (value is >= 1000 and <= 1720)
                    Rotation = value - 1360;
                else
                    // V2LegacyRotationEventLoadingTest covers legacy high values that must clamp to the last valid
                    // rotation instead of using the array length as an out-of-range index and aborting the map load.
                    Rotation = lightValueToRotationDegrees[
                        Math.Clamp(value, 0, lightValueToRotationDegrees.Length - 1)];
            }
        }

        private static readonly int[] lightValueToRotationDegrees = { -60, -45, -30, -15, 15, 30, 45, 60 };

        public override bool HasMatchingTrack(string filter) => true;


        public override bool IsNoodleExtensions() =>
            CustomData != null
            && CustomData.HasKey("_rotation")
            && CustomData["_rotation"].IsNumber;

        public override bool IsMappingExtensions() => Value is >= 1000 and <= 1720;
        public override string CustomKeyColor => "_unused";
        public override string CustomKeyTrack => "_unused";

        public Vector2? GetPosition()
        {
            var x = ExecutionTime == ExecutionTime.Late ? 1 : 0;
            return new Vector2(
                x + 0.5f,
                0.5f);
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseRotationEvent @event) return Type == @event.Type;
            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseRotationEvent evt)
            {
                ExecutionTime = evt.ExecutionTime;
                Rotation = evt.Rotation;
            }
        }

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            if (other is not BaseRotationEvent evt) return comparison;
            if (comparison == 0) comparison = ExecutionTime.CompareTo(evt.ExecutionTime);
            if (comparison == 0) comparison = Rotation.CompareTo(evt.Rotation);

            return comparison;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                2 => V2RotationEvent.ToJson(this),
                3 or 4 => Type switch
                {
                    _ => V3RotationEvent.ToJson(this),
                }
            };

        public override BaseItem Clone()
        {
            var evt = new BaseRotationEvent(this);
            evt.ParseCustom();
            return evt;
        }
    }
}
