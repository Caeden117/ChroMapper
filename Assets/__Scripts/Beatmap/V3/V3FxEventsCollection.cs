using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3FxEventsCollection
    {
        public static BaseFxEventsCollection GetFromJson(JSONNode node)
        {
            var fxEventsCollection = new BaseFxEventsCollection();
            
            if (node.HasKey("_il"))
            {
                fxEventsCollection.IntFxEvents = node["_il"].AsArray.Linq.Select(childNode => V3IntFxEvent.GetFromJson(childNode.Value)).ToArray();
            }

            if (node.HasKey("_fl"))
            {
                fxEventsCollection.FloatFxEvents = node["_fl"].AsArray.Linq.Select(childNode => V3FloatFxEvent.GetFromJson(childNode.Value)).ToArray();
            }

            return fxEventsCollection;
        }

        public static JSONNode ToJson(BaseFxEventsCollection fxEventsCollection)
        {
            var node = new JSONObject();

            node["_il"] = new JSONArray();
            foreach (var intFxEvent in fxEventsCollection.IntFxEvents) node["_il"].Add(intFxEvent.ToJson());

            node["_fl"] = new JSONArray();
            foreach (var floatFxEvent in fxEventsCollection.FloatFxEvents) node["_fl"].Add(floatFxEvent.ToJson());

            return node;
        }
    }
}
