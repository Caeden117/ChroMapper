using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3RotationEvent
    {
        public static BaseRotationEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseRotationEvent();

            evt.JsonTime = node["b"].AsFloat;
            evt.ExecutionTime = node["e"].AsInt == 0 ? ExecutionTime.Early : ExecutionTime.Late;
            evt.Rotation = node["r"].AsFloat;
            evt.CustomData = node["customData"];

            return evt;
        }

        public static JSONNode ToJson(BaseRotationEvent evt)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(evt.JsonTime);
            node["e"] = (int)evt.ExecutionTime;
            node["r"] = evt.Rotation;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["customData"] = evt.CustomData;
            return node;
        }
    }
}
