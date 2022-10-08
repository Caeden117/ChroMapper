using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BasicEventTypesForKeywords : BaseEventTypesForKeywords
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
            var ary = new JSONArray();
            foreach (var i in Events) ary.Add(i);
            node["e"] = ary;
            return node;
        }

        public override BaseItem Clone() => new V3BasicEventTypesForKeywords(ToJson().Clone());
    }
}
