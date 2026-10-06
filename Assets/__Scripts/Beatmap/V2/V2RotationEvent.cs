using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2RotationEvent
    {
        public static BaseRotationEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseRotationEvent();
            
            evt.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            evt.Type = BaseItem.GetRequiredNode(node, "_type").AsInt;
            evt.Value = BaseItem.GetRequiredNode(node, "_value").AsInt;
            evt.CustomData = node["_customData"];

            return evt;
        }
        
        public static JSONNode ToJson(BaseRotationEvent evt)
        {
            JSONNode node = new JSONObject();
            node["_time"] = JSONNumber.RoundBeat(evt.JsonTime);
            node["_type"] = evt.Type;
            node["_value"] = evt.Value;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["_customData"] = evt.CustomData;
            return node;
        }
    }
}
