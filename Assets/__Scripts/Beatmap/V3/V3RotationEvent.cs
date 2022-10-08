using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3RotationEvent : IRotationEvent
    {
        public V3RotationEvent()
        {
        }

        public V3RotationEvent(IRotationEvent other) : base(other)
        {
        }

        public V3RotationEvent(IEvent evt) : base(evt)
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

        public override string CustomKeyColor { get; } = "color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["e"] = ExecutionTime;
            node["r"] = Rotation;
            if (CustomData == null) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V3RotationEvent(Time, ExecutionTime, Rotation, CustomData?.Clone());
    }
}
