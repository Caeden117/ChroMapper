using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V2
{
    public static class V2BpmEvent
    {
        public const string CustomKeyTrack = "_track";
        public const string CustomKeyColor = "_color";

        public static BaseBpmEvent GetFromJson(JSONNode node)
        {
            var bpmEvent = new BaseBpmEvent();

            var bpm =  BaseItem.GetRequiredNode(node, "_floatValue").AsFloat;
            
            bpmEvent.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            bpmEvent.Bpm = bpm;
            bpmEvent.Type = 100;
            bpmEvent.FloatValue = bpm;
            bpmEvent.CustomData = node["_customData"];

            return bpmEvent;
        }

        public static JSONNode ToJson(BaseBpmEvent bpmEvent)
        {
            JSONNode node = new JSONObject();
            node["_time"] = new JSONNumberWithOverridenRounding(bpmEvent.JsonTime, Settings.Instance.BpmTimeValueDecimalPrecision);
            node["_type"] = bpmEvent.Type;
            node["_value"] = 0;
            node["_floatValue"] = bpmEvent.Bpm;
            bpmEvent.CustomData = bpmEvent.SaveCustom();
            if (bpmEvent.CustomData.Children.Any()) return node;
            node["_customData"] = bpmEvent.CustomData;
            return node;
        }
    }
}
