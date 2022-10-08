using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2SpecialEventsKeywordFiltersKeywords : IEventTypesForKeywords
    {
        public V2SpecialEventsKeywordFiltersKeywords()
        {
        }

        public V2SpecialEventsKeywordFiltersKeywords(IEventTypesForKeywords other) : base(other)
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
            var ary = new JSONArray();
            foreach (var i in Events) ary.Add(i);
            node["_specialEvents"] = ary;
            return node;
        }

        public override IItem Clone() => new V2SpecialEventsKeywordFiltersKeywords(ToJson().Clone());
    }
}
