using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2SpecialEventsKeywordFilters : BaseEventTypesWithKeywords
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
            var ary = new JSONArray();
            foreach (var k in Keywords) ary.Add(k.ToJson());
            node["_keywords"] = ary;
            return node;
        }

        public override BaseItem Clone() => new V2SpecialEventsKeywordFilters(ToJson().Clone());
    }
}
