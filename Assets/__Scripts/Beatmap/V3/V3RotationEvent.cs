using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3RotationEvent : BaseRotationEvent
    {
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
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            ExecutionTime = RetrieveRequiredNode(node, "e").AsInt;
            Rotation = RetrieveRequiredNode(node, "r").AsFloat;
            CustomData = node["customData"];
        }

        public V3RotationEvent(float time, int executionTime, float rotation, JSONNode customData = null) : base(time,
            executionTime, rotation, customData)
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

        public override string CustomKeyStepMult { get; } = "stepMult";

        public override string CustomKeyPropMult { get; } = "propMult";

        public override string CustomKeySpeedMult { get; } = "speedMult";

        public override string CustomKeyPreciseSpeed { get; } = "preciseSpeed";

        public override string CustomKeyDirection { get; } = "direction";

        public override string CustomKeyLockRotation { get; } = "lockRotation";

        public override string CustomKeyLaneRotation { get; } = "rotation";

        public override string CustomKeyNameFilter { get; } = "nameFilter";

        public override bool IsPropagation { get; } = false;

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["e"] = ExecutionTime;
            node["r"] = Rotation;
            SaveCustom();
            if (CustomData.Count == 0) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3RotationEvent(Time, ExecutionTime, Rotation, CustomData?.Clone());
    }
}
