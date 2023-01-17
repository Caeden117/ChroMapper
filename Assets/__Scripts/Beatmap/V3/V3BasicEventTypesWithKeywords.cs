using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BasicEventTypesWithKeywords : BaseEventTypesWithKeywords
    {
        public V3BasicEventTypesWithKeywords()
        {
        }

        public V3BasicEventTypesWithKeywords(BaseEventTypesWithKeywords other) : base(other)
        {
        }

        public V3BasicEventTypesWithKeywords(JSONNode node)
        {
            if (node.HasKey("d"))
                Keywords = node["d"].AsArray.Linq
                    .Select(x => new V3BasicEventTypesForKeywords(x) as BaseEventTypesForKeywords).ToArray();
            else Keywords = new BaseEventTypesForKeywords[]{};
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["d"] = new JSONArray();
            foreach (var k in Keywords) node["d"].Add(k.ToJson());
            return node;
        }

        public override BaseItem Clone() => new V3BasicEventTypesWithKeywords(ToJson());
    }
}
