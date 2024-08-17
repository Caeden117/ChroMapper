using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.V3
{
    public class V3RotationEvent
    {
        public static BaseEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;
            evt.Type = (int)(node["e"].AsInt == 0 ? EventTypeValue.EarlyLaneRotation : EventTypeValue.LateLaneRotation);
            evt.FloatValue = node["r"].AsFloat;
            evt.CustomData = node["customData"];
            evt.RefreshCustom();

            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;
            node["e"] = evt.Type == (int)EventTypeValue.EarlyLaneRotation ? 0 : 1;
            node["r"] = evt.FloatValue;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["customData"] = evt.CustomData;
            return node;
        }
    }
}
