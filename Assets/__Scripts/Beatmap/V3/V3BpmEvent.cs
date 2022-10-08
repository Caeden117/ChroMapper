using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3BpmEvent : IBpmEvent
    {
        public V3BpmEvent()
        {
        }

        public V3BpmEvent(IBpmEvent other) : base(other)
        {
        }

        public V3BpmEvent(IEvent evt) : base(evt)
        {
        }

        public V3BpmEvent(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            Bpm = RetrieveRequiredNode(node, "m").AsFloat;
            CustomData = node["customData"];
        }

        public V3BpmEvent(float time, float bpm, JSONNode customData = null) : base(time, bpm, customData)
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
            node["m"] = Bpm;
            if (CustomData == null) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V3BpmEvent(Time, Bpm, CustomData?.Clone());
    }
}
