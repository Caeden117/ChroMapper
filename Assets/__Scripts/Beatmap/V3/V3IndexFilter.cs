using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3IndexFilter
    {
        public static BaseIndexFilter GetFromJson(JSONNode node)
        {
            var filter = new BaseIndexFilter();
            
            filter.Type = node["f"].AsInt;
            filter.Param0 = node["p"].AsInt;
            filter.Param1 = node["t"].AsInt;
            filter.Reverse = node["r"].AsInt;
            filter.Chunks = node["c"].AsInt;
            filter.Random = node["n"].AsInt;
            filter.Seed = node["s"].AsInt;
            filter.Limit = node["l"].AsFloat;
            filter.LimitAffectsType = node["d"].AsInt;

            return filter;
        }

        public static JSONNode ToJson(BaseIndexFilter filter) =>
            new JSONObject
            {
                ["f"] = filter.Type,
                ["p"] = filter.Param0,
                ["t"] = filter.Param1,
                ["r"] = filter.Reverse,
                ["c"] = filter.Chunks,
                ["n"] = filter.Random,
                ["s"] = filter.Seed,
                ["l"] = filter.Limit,
                ["d"] = filter.LimitAffectsType
            };
    }
}
