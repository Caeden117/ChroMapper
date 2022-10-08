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

        public V3BasicEventTypesWithKeywords(JSONNode node) =>
            Keywords = RetrieveRequiredNode(node, "d").AsArray.Linq
                .Select(x => new V3BasicEventTypesForKeywords(x)).ToArray();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            var ary = new JSONArray();
            foreach (var k in Keywords) ary.Add(k.ToJson());
            node["d"] = ary;
            return node;
        }

        public override BaseItem Clone() => new V3BasicEventTypesWithKeywords(ToJson().Clone());
    }
}
