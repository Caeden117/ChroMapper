using System;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3ColorBoostEvent : IColorBoostEvent
    {
        public V3ColorBoostEvent()
        {
        }

        public V3ColorBoostEvent(IColorBoostEvent other) : base(other)
        {
        }

        public V3ColorBoostEvent(IEvent evt) : base(evt)
        {
        }

        public V3ColorBoostEvent(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            Toggle = RetrieveRequiredNode(node, "o").AsBool;
            CustomData = node["customData"];
        }

        public V3ColorBoostEvent(float time, bool toggle, JSONNode customData = null) : base(time, toggle, customData)
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
            node["o"] = Toggle;
            if (CustomData != null) node["customData"] = CustomData;
            return node;
        }

        public override IItem Clone() => new V3ColorBoostEvent(Time, Toggle, CustomData?.Clone());
    }
}
