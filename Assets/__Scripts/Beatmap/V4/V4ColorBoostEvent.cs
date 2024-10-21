using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4ColorBoostEvent
    {
        public static BaseEvent GetFromJson(JSONNode node, IList<V4CommonData.ColorBoostEvent> colorBoostEventsCommonData)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;
            evt.Type = (int)EventTypeValue.ColorBoost;

            var index = node["i"].AsInt;
            var eventData = colorBoostEventsCommonData[index];

            evt.Value = eventData.Boost;
            
            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt, IList<V4CommonData.ColorBoostEvent> colorBoostEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;

            var data = V4CommonData.ColorBoostEvent.FromBaseEvent(evt);
            node["i"] = colorBoostEventsCommonData.IndexOf(data);
            
            return node;
        }
    }
}
