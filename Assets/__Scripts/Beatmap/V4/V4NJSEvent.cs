using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4NJSEvent
    {
        public static BaseNJSEvent GetFromJson(JSONNode node, IList<V4CommonData.NJSEvent> njsEventsCommonData)
        {
            var njsEvent = new BaseNJSEvent();
            
            njsEvent.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var njsEventData = njsEventsCommonData[index];

            njsEvent.UsePrevious = njsEventData.UsePrevious;
            njsEvent.Easing = njsEventData.Easing;
            njsEvent.RelativeNJS = njsEventData.RelativeNJS;
            
            return njsEvent;
        }

        public static JSONNode ToJson(BaseNJSEvent njsEvent, IList<V4CommonData.NJSEvent> njsEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = njsEvent.JsonTime;

            var data = V4CommonData.NJSEvent.FromBaseNJSEvent(njsEvent);
            node["i"] = njsEventsCommonData.IndexOf(data);
            
            return node;
        }
    }
}
