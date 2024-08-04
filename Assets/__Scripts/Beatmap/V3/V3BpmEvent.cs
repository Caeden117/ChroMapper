using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public static class V3BpmEvent
    {
        public static BaseBpmEvent GetFromJson(JSONNode node)
        {
            var bpmEvent = new BaseBpmEvent();

            var bpm = BaseItem.GetRequiredNode(node, "m").AsFloat;

            bpmEvent.JsonTime = node["b"];
            bpmEvent.Bpm = bpm;
            bpmEvent.Type = 100;
            bpmEvent.FloatValue = bpm;
            bpmEvent.CustomData = node["customData"];

            return bpmEvent;
        }
        
        public static JSONNode ToJson(BaseBpmEvent bpmEvent)
        {
            JSONNode node = new JSONObject();
            node["b"] = new JSONNumberWithOverridenRounding(bpmEvent.JsonTime, Settings.Instance.BpmTimeValueDecimalPrecision);
            node["m"] = bpmEvent.Bpm;
            bpmEvent.CustomData = bpmEvent.SaveCustom();
            if (!bpmEvent.CustomData.Children.Any()) return node;
            node["customData"] = bpmEvent.CustomData;
            return node;
        }
    }
}
