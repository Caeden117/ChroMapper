using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4RotationEvent
    {
        public static BaseEvent GetFromJson(JSONNode node, IList<V4CommonData.RotationEvent> rotationsCommonData)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var rotationData = rotationsCommonData[index];

            evt.Type = (int)(rotationData.Type == 0 ? EventTypeValue.EarlyLaneRotation : EventTypeValue.LateLaneRotation);
            evt.Rotation = rotationData.Rotation;
            
            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt, IList<V4CommonData.RotationEvent> rotationsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;

            var data = V4CommonData.RotationEvent.FromBaseEvent(evt);
            node["i"] = rotationsCommonData.IndexOf(data);
            
            return node;
        }
    }
}
