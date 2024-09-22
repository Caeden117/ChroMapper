using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public static class V2SpecialEventsKeywordFilters
    {
        public static BaseEventTypesWithKeywords GetFromJson(JSONNode node)
        {
            var withKeywords = new BaseEventTypesWithKeywords();
            
            withKeywords.Keywords = BaseItem.GetRequiredNode(node, "_keywords").AsArray.Linq
                .Select(x => V2SpecialEventsKeywordFiltersKeywords.GetFromJson(x.Value)).ToArray();

            return withKeywords;
        }

        public static JSONNode ToJson(BaseEventTypesWithKeywords withKeywords)
        {
            JSONNode node = new JSONObject();
            node["_keywords"] = new JSONArray();
            foreach (var k in withKeywords.Keywords) node["_keywords"].Add(V2SpecialEventsKeywordFiltersKeywords.ToJson(k));
            return node;
        }
    }
}
