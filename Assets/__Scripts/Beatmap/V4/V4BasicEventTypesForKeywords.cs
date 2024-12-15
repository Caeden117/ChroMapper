using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V4
{
    public class V4BasicEventTypesForKeywords
    {
        public static BaseEventTypesForKeywords GetFromJson(JSONNode node)
        {
            var forKeywords = new BaseEventTypesForKeywords();
            
            forKeywords.Keyword = BaseItem.GetRequiredNode(node, "k");
            forKeywords.Events = BaseItem.GetRequiredNode(node, "e").AsArray.Linq
                .Select(x => x.Value.AsInt).ToArray();

            return forKeywords;
        }

        public static JSONNode ToJson(BaseEventTypesForKeywords forKeywords)
        {
            JSONNode node = new JSONObject();
            node["k"] = forKeywords.Keyword;
            node["e"] = new JSONArray();
            foreach (var i in forKeywords.Events) node["e"].Add(i);
            return node;
        }
    }
}
