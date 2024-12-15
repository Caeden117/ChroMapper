using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3LightTranslationEventBox
    {
        public static BaseLightTranslationEventBox GetFromJson(JSONNode node)
        {
            var box = new BaseLightTranslationEventBox();
            
            box.IndexFilter = V3IndexFilter.GetFromJson(BaseItem.GetRequiredNode(node, "f"));
            box.BeatDistribution = node["w"].AsFloat;
            box.BeatDistributionType = node["d"].AsInt;
            box.TranslationDistribution = node["s"].AsFloat;
            box.TranslationDistributionType = node["t"].AsInt;
            box.TranslationAffectFirst = node["b"].AsInt;
            box.Axis = node["a"].AsInt;
            box.Flip = node["r"].AsInt;
            box.Easing = node["i"].AsInt;
            box.Events = BaseItem.GetRequiredNode(node, "l").AsArray.Linq.Select(x => V3LightTranslationBase.GetFromJson(x.Value)).ToArray();

            return box;
        }

        public static JSONNode ToJson(BaseLightTranslationEventBox box)
        {
            JSONNode node = new JSONObject();
            node["f"] = box.IndexFilter.ToJson();
            node["w"] = box.BeatDistribution;
            node["d"] = box.BeatDistributionType;
            node["s"] = box.TranslationDistribution;
            node["t"] = box.TranslationDistributionType;
            node["b"] = box.TranslationAffectFirst;
            node["a"] = box.Axis;
            node["r"] = box.Flip;
            node["i"] = box.Easing;
            var ary = new JSONArray();
            foreach (var k in box.Events) ary.Add(V3LightTranslationBase.ToJson(k));
            node["l"] = ary;
            return node;
        }
    }
}
