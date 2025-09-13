using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V4;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3VfxEventEventBox
    {
        public static BaseVfxEventEventBox GetFromJson(JSONNode node, IList<FloatFxEventBase> floatFxEvents)
        {
            var vfxBox = new BaseVfxEventEventBox();
            
            vfxBox.IndexFilter = V3IndexFilter.GetFromJson(BaseItem.GetRequiredNode(node, "f"));
            vfxBox.BeatDistribution = node["w"].AsFloat;
            vfxBox.BeatDistributionType = node["d"].AsInt;
            vfxBox.VfxDistribution = node["s"].AsFloat;
            vfxBox.VfxDistributionType = node["t"].AsInt;
            vfxBox.VfxAffectFirst = node["b"].AsInt;
            vfxBox.Easing = node["i"].AsInt;

            if (node.HasKey("l"))
            {
                vfxBox.FloatFxEvents = node["l"].AsArray.Linq.Select(x =>
                { 
                    var floatFxIndex = x.Value.AsInt;
                    var floatFxEvent = (FloatFxEventBase)floatFxEvents[floatFxIndex].Clone();
                    return floatFxEvent;
                }).ToList();
            }

            return vfxBox;
        }

        public static JSONNode ToJson(BaseVfxEventEventBox vfxBox, IList<FloatFxEventBase> floatFxEvents)
        {
            JSONNode node = new JSONObject();
            node["f"] = vfxBox.IndexFilter.ToJson();
            node["w"] = vfxBox.BeatDistribution;
            node["d"] = vfxBox.BeatDistributionType;
            node["s"] = vfxBox.VfxDistribution;
            node["t"] = vfxBox.VfxDistributionType;
            node["b"] = vfxBox.VfxAffectFirst;
            node["i"] = vfxBox.Easing;

            node["l"] = new JSONArray();
            foreach (var data in vfxBox.FloatFxEvents)
            {
                node["l"].Add(floatFxEvents.IndexOf(data));
            }

            return node;
        }
    }
}
