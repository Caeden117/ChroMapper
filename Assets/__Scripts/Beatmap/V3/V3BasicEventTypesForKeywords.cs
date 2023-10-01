using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BasicEventTypesForKeywords : BaseEventTypesForKeywords, V3Object
    {
        public V3BasicEventTypesForKeywords()
        {
        }

        public V3BasicEventTypesForKeywords(BaseEventTypesForKeywords other) : base(other)
        {
        }

        public V3BasicEventTypesForKeywords(JSONNode node)
        {
            Keyword = RetrieveRequiredNode(node, "k");
            Events = RetrieveRequiredNode(node, "e").AsArray.Linq
                .Select(x => x.Value.AsInt).ToArray();
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["k"] = Keyword;
            node["e"] = new JSONArray();
            foreach (var i in Events) node["e"].Add(i);
            return node;
        }

        public override BaseItem Clone() => new V3BasicEventTypesForKeywords(ToJson().Clone());
    }
}
