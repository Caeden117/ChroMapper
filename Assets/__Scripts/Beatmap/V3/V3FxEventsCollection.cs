using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3FxEventsCollection : BaseFxEventsCollection, V3Object
    {
        public V3FxEventsCollection()
        {
        }

        public V3FxEventsCollection(JSONNode node)
        {
            if (node.HasKey("il"))
            {
                IntFxEvents = node["il"].AsArray.Linq.Select(childNode => new V3IntFxEvent(childNode)).ToArray();
            }

            if (node.HasKey("fl"))
            {
                FloatFxEvents = node["fl"].AsArray.Linq.Select(childNode => new V3FloatFxEvent(childNode)).ToArray();
            }
        }

        public V3FxEventsCollection(V3FxEventsCollection other)
        {
            var newFxEventsCollection = new V3FxEventsCollection();
            foreach (var intFxEvent in IntFxEvents) newFxEventsCollection.IntFxEvents.Append(intFxEvent.Clone());
            foreach (var floatFxEvent in FloatFxEvents) newFxEventsCollection.FloatFxEvents.Append(floatFxEvent.Clone());
        }

        public override JSONNode ToJson()
        {
            var node = new JSONObject();

            node["il"] = new JSONArray();
            foreach (var intFxEvent in IntFxEvents) node["il"].Add(intFxEvent.ToJson());

            node["fl"] = new JSONArray();
            foreach (var floatFxEvent in FloatFxEvents) node["fl"].Add(floatFxEvent.ToJson());

            return node;
        }

        public override BaseItem Clone() => new V3FxEventsCollection(this);
    }
}
