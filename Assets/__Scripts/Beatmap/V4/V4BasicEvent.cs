using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4BasicEvent
    {
        public static BaseEvent GetFromJson(JSONNode node, IList<V4CommonData.BasicEvent> basicEventsCommonData)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var eventData = basicEventsCommonData[index];

            evt.Type = eventData.Type;
            evt.Value = eventData.Value;
            evt.FloatValue = eventData.FloatValue;
            
            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt, IList<V4CommonData.BasicEvent> basicEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;

            var data = V4CommonData.BasicEvent.FromBaseEvent(evt);
            node["i"] = basicEventsCommonData.IndexOf(data);
            
            return node;
        }
    }
}
