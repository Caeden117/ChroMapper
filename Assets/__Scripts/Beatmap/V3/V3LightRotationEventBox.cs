using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3LightRotationEventBox
    {
        public static BaseLightRotationEventBox GetFromJson(JSONNode node)
        {
            var box = new BaseLightRotationEventBox();
            
            box.IndexFilter = V3IndexFilter.GetFromJson(BaseItem.GetRequiredNode(node, "f"));
            box.BeatDistribution = node["w"].AsFloat;
            box.BeatDistributionType = node["d"].AsInt;
            box.RotationDistribution = node["s"].AsFloat;
            box.RotationDistributionType = node["t"].AsInt;
            box.RotationAffectFirst = node["b"].AsInt;
            box.Axis = node["a"].AsInt;
            box.Flip = node["r"].AsInt;
            box.Easing = node["i"].AsInt;
            box.Events = BaseItem.GetRequiredNode(node, "l").AsArray.Linq.Select(x => V3LightRotationBase.GetFromJson(x.Value)).ToArray();

            return box;
        }

        public static JSONNode ToJson(BaseLightRotationEventBox box)
        {
            JSONNode node = new JSONObject();
            node["f"] = box.IndexFilter.ToJson();
            node["w"] = box.BeatDistribution;
            node["d"] = box.BeatDistributionType;
            node["s"] = box.RotationDistribution;
            node["t"] = box.RotationDistributionType;
            node["b"] = box.RotationAffectFirst;
            node["a"] = box.Axis;
            node["r"] = box.Flip;
            node["i"] = box.Easing;
            var ary = new JSONArray();
            foreach (var k in box.Events) ary.Add(V3LightRotationBase.ToJson(k));
            node["l"] = ary;
            return node;
        }
    }
}
