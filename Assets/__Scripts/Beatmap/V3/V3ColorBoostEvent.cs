using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3ColorBoostEvent
    {
        public static BaseEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;
            evt.Type = 5;
            evt.Value = node["o"].AsBool ? 1 : 0;
            evt.FloatValue = 0;
            evt.CustomData = node["customData"];
            
            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;
            node["o"] = evt.Value == 1;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["customData"] = evt.CustomData;
            return node;
        }
    }
}
