using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V4
{
    public class V4BasicEventTypesWithKeywords
    {
        public static BaseEventTypesWithKeywords GetFromJson(JSONNode node)
        {
            var withKeywords = new BaseEventTypesWithKeywords();
            
            if (node.HasKey("d"))
                withKeywords.Keywords = node["d"].AsArray.Linq
                    .Select(x => V4BasicEventTypesForKeywords.GetFromJson(x.Value)).ToArray();
            else withKeywords.Keywords = new BaseEventTypesForKeywords[]{};

            return withKeywords;
        }

        public static JSONNode ToJson(BaseEventTypesWithKeywords withKeywords)
        {
            JSONNode node = new JSONObject();
            node["d"] = new JSONArray();
            foreach (var k in withKeywords.Keywords) node["d"].Add(V4BasicEventTypesForKeywords.ToJson(k));
            return node;
        }
    }
}
