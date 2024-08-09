using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2SpecialEventsKeywordFiltersKeywords
    {
        public static BaseEventTypesForKeywords GetFromJson(JSONNode node)
        {
            var forKeywords = new BaseEventTypesForKeywords();
            
            forKeywords.Keyword = BaseItem.GetRequiredNode(node, "_keyword");
            forKeywords.Events = BaseItem.GetRequiredNode(node, "_specialEvents").AsArray.Linq
                .Select(x => x.Value.AsInt).ToArray();

            return forKeywords;
        }

        public static JSONNode ToJson(BaseEventTypesForKeywords forKeywords)
        {
            JSONNode node = new JSONObject();
            node["_keyword"] = forKeywords.Keyword;
            node["_specialEvents"] = new JSONArray();
            foreach (var i in forKeywords.Events) node["_specialEvents"].Add(i);
            return node;
        }
    }
}
