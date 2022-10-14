using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3LightRotationEventBoxGroup : BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>
    {
        public V3LightRotationEventBoxGroup()
        {
        }

        public V3LightRotationEventBoxGroup(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            ID = RetrieveRequiredNode(node, "g").AsInt;
            Events = new List<BaseLightRotationEventBox>(RetrieveRequiredNode(node, "e").AsArray.Linq
                .Select(x => new V3LightRotationEventBox(x)).ToList());
            CustomData = node["customData"];
        }

        public V3LightRotationEventBoxGroup(float time, int id, List<BaseLightRotationEventBox> events,
            JSONNode customData = null) : base(time, id, events, customData)
        {
        }

        public override Color? CustomColor
        {
            get => null;
            set { }
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["g"] = ID;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["e"] = ary;
            CustomData = SaveCustom();
            if (CustomData.Count == 0) return node;
            node["customData"] = CustomData;
            return node;
        }

        // TODO: proper event box group cloning
        public override BaseItem Clone() => new V3LightRotationEventBoxGroup(ToJson().Clone());
    }
}
