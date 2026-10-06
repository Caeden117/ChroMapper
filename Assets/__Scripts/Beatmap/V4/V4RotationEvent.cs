using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4RotationEvent
    {
        public static BaseRotationEvent GetFromJson(
            JSONNode node,
            IList<V4CommonData.RotationEvent> rotationsCommonData)
        {
            var evt = new BaseRotationEvent();

            evt.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var rotationData = rotationsCommonData[index];

            evt.ExecutionTime = (ExecutionTime)Math.Clamp(rotationData.ExecutionTime, 0, 1);
            evt.Rotation = rotationData.Rotation;

            return evt;
        }

        public static JSONNode ToJson(BaseRotationEvent evt, IList<V4CommonData.RotationEvent> rotationsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(evt.JsonTime);

            var data = V4CommonData.RotationEvent.FromBaseEvent(evt);
            node["i"] = rotationsCommonData.IndexOf(data);

            return node;
        }
    }
}
