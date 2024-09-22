using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3LightColorEventBox
    {
        public static BaseLightColorEventBox GetFromJson(JSONNode node)
        {
            var box = new BaseLightColorEventBox();
            
            box.IndexFilter = V3IndexFilter.GetFromJson(BaseItem.GetRequiredNode(node, "f"));
            box.BeatDistribution = node["w"].AsFloat;
            box.BeatDistributionType = node["d"].AsInt;
            box.BrightnessDistribution = node["r"].AsFloat;
            box.BrightnessDistributionType = node["t"].AsInt;
            box.BrightnessAffectFirst = node["b"].AsInt;
            box.Easing = node["i"].AsInt;
            box.Events = BaseItem.GetRequiredNode(node, "e").AsArray.Linq.Select(x => V3LightColorBase.GetFromJson(x.Value)).ToArray();

            return box;
        }

        public static JSONNode ToJson(BaseLightColorEventBox box)
        {
            JSONNode node = new JSONObject();
            node["f"] = box.IndexFilter.ToJson();
            node["w"] = box.BeatDistribution;
            node["d"] = box.BeatDistributionType;
            node["r"] = box.BrightnessDistribution;
            node["t"] = box.BrightnessDistributionType;
            node["b"] = box.BrightnessAffectFirst;
            node["i"] = box.Easing;
            var ary = new JSONArray();
            foreach (var k in box.Events) ary.Add(V3LightColorBase.ToJson(k));
            node["e"] = ary;
            return node;
        }
    }
}
