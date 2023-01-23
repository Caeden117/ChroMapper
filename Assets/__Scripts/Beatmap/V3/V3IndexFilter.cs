using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3IndexFilter : BaseIndexFilter, V3Object
    {
        public V3IndexFilter()
        {
        }

        public V3IndexFilter(JSONNode node)
        {
            Type = RetrieveRequiredNode(node, "f").AsInt;
            Param0 = RetrieveRequiredNode(node, "p").AsInt;
            Param1 = RetrieveRequiredNode(node, "t").AsInt;
            Reverse = RetrieveRequiredNode(node, "r").AsInt;
            Chunks = node["c"]?.AsInt ?? 0;
            Random = node["n"]?.AsInt ?? 0;
            Seed = node["s"]?.AsInt ?? 0;
            Limit = node["l"]?.AsFloat ?? 0f;
            LimitAffectsType = node["d"]?.AsInt ?? 0;
        }

        public V3IndexFilter(int type, int param0, int param1, int reverse) : base(type, param0, param1, reverse)
        {
        }

        public V3IndexFilter(int type, int param0, int param1, int reverse, int chunks = 0, float limit = 0,
            int limitAffectsType = 0, int random = 0, int seed = 0) : base(type, param0, param1, reverse, chunks, limit,
            limitAffectsType, random, seed)
        {
        }

        public override JSONNode ToJson() =>
            new JSONObject
            {
                ["f"] = Type,
                ["p"] = Param0,
                ["t"] = Param1,
                ["r"] = Reverse,
                ["c"] = Chunks,
                ["n"] = Random,
                ["s"] = Seed,
                ["l"] = Limit,
                ["d"] = LimitAffectsType
            };

        public override BaseItem Clone() =>
            new V3IndexFilter(Type, Param0, Param1, Reverse, Chunks, Limit, LimitAffectsType, Random, Seed);
    }
}
