using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3RotationEvent : BaseRotationEvent, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V3RotationEvent()
        {
        }

        public V3RotationEvent(BaseRotationEvent other) : base(other)
        {
        }

        public V3RotationEvent(BaseEvent evt) : base(evt)
        {
        }

        public V3RotationEvent(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "b").AsFloat;
            ExecutionTime = RetrieveRequiredNode(node, "e").AsInt;
            Rotation = RetrieveRequiredNode(node, "r").AsFloat;
            Type = (int)(ExecutionTime == 0 ? EventTypeValue.EarlyLaneRotation : EventTypeValue.LateLaneRotation);
            CustomData = node["customData"];
        }

        public V3RotationEvent(float time, int executionTime, float rotation, JSONNode customData = null) : base(time,
            executionTime, rotation, customData)
        {
        }

        public V3RotationEvent(float jsonTime, float songBpmTime, int executionTime, float rotation,
            JSONNode customData = null) : base(jsonTime, songBpmTime, executionTime, rotation, customData)
        {
        }

        public override Color? CustomColor
        {
            get => null;
            set { }
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyPropID { get; } = "propID";

        public override string CustomKeyLightID { get; } = "lightID";

        public override string CustomKeyLerpType { get; } = "lerpType";

        public override string CustomKeyEasing { get; } = "easing";

        public override string CustomKeyLightGradient { get; } = "lightGradient";

        public override string CustomKeyStep { get; } = "step";

        public override string CustomKeyProp { get; } = "prop";

        public override string CustomKeySpeed { get; } = "speed";

        public override string CustomKeyRingRotation { get; } = "rotation";

        public override string CustomKeyStepMult { get; } = "stepMult";

        public override string CustomKeyPropMult { get; } = "propMult";

        public override string CustomKeySpeedMult { get; } = "speedMult";

        public override string CustomKeyPreciseSpeed { get; } = "preciseSpeed";

        public override string CustomKeyDirection { get; } = "direction";

        public override string CustomKeyLockRotation { get; } = "lockRotation";

        public override string CustomKeyLaneRotation { get; } = "rotation";

        public override string CustomKeyNameFilter { get; } = "nameFilter";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(JsonTime, DecimalPrecision);
            node["e"] = ExecutionTime;
            node["r"] = Rotation;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3RotationEvent(JsonTime, SongBpmTime, ExecutionTime, Rotation, SaveCustom().Clone());
    }
}
