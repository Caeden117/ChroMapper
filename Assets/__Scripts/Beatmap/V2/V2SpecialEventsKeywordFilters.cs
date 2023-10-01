using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2SpecialEventsKeywordFilters : BaseEventTypesWithKeywords, V2Object
    {
        public V2SpecialEventsKeywordFilters()
        {
        }

        public V2SpecialEventsKeywordFilters(BaseEventTypesWithKeywords other) : base(other)
        {
        }

        public V2SpecialEventsKeywordFilters(JSONNode node) =>
            Keywords = RetrieveRequiredNode(node, "_keywords").AsArray.Linq
                .Select(x => new V2SpecialEventsKeywordFiltersKeywords(x)).ToArray();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_keywords"] = new JSONArray();
            foreach (var k in Keywords) node["_keywords"].Add(k.ToJson());
            return node;
        }

        public override BaseItem Clone() => new V2SpecialEventsKeywordFilters(ToJson().Clone());
    }
}
