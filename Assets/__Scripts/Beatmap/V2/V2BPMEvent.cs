using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V2
{
    public class V2BpmEvent : BaseBpmEvent, V2Object
    {
        public V2BpmEvent()
        {
        }

        public V2BpmEvent(BaseBpmEvent other) : base(other)
        {
        }

        public V2BpmEvent(BaseEvent evt) : base(evt)
        {
        }

        public V2BpmEvent(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "_time").AsFloat;
            Bpm = RetrieveRequiredNode(node, "_floatValue").AsFloat;
            Type = 100;
            FloatValue = Bpm;
            CustomData = node["_customData"];
        }

        public V2BpmEvent(float time, float bpm, JSONNode customData = null) : base(time, bpm, customData)
        {
        }

        public V2BpmEvent(float jsonTime, float songBpmTime, float bpm, JSONNode customData = null) :
            base(jsonTime, songBpmTime, bpm, customData)
        {
        }

        public override Color? CustomColor
        {
            get => null;
            set { }
        }

        public override string CustomKeyTrack { get; } = "_track";
        public override string CustomKeyColor { get; } = "_color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(JsonTime, DecimalPrecision);
            node["_type"] = Type;
            node["_value"] = 0;
            node["_floatValue"] = Bpm;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V2BpmEvent(JsonTime, SongBpmTime, Bpm, SaveCustom().Clone());
    }
}
