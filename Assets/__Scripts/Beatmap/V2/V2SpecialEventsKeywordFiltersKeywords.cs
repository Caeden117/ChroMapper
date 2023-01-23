using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2SpecialEventsKeywordFiltersKeywords : BaseEventTypesForKeywords, V2Object
    {
        public V2SpecialEventsKeywordFiltersKeywords()
        {
        }

        public V2SpecialEventsKeywordFiltersKeywords(BaseEventTypesForKeywords other) : base(other)
        {
        }

        public V2SpecialEventsKeywordFiltersKeywords(JSONNode node)
        {
            Keyword = RetrieveRequiredNode(node, "_keyword");
            Events = RetrieveRequiredNode(node, "_specialEvents").AsArray.Linq
                .Select(x => x.Value.AsInt).ToArray();
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_keyword"] = Keyword;
            node["_specialEvents"] = new JSONArray();
            foreach (var i in Events) node["_specialEvents"].Add(i);
            return node;
        }

        public override BaseItem Clone() => new V2SpecialEventsKeywordFiltersKeywords(ToJson());
    }
}
