using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V4;
using SimpleJSON;
using Debug = UnityEngine.Debug;

namespace Beatmap.V3
{
    public static class V3VfxEventEventBox
    {
        public static BaseVfxEventEventBox GetFromJson(JSONNode node, IList<BaseFxEventFloat> floatFxEvents)
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
                vfxBox.Events = node["l"].AsArray.Linq.Select(x =>
                { 
                    var floatFxIndex = x.Value.AsInt;
                    var floatFxEvent = (BaseFxEventFloat)floatFxEvents[floatFxIndex].Clone();
                    return floatFxEvent;
                }).ToArray();
            }

            return vfxBox;
        }

        public static JSONNode ToJson(BaseVfxEventEventBox vfxBox, IList<BaseFxEventFloat> floatFxEvents)
        {
            JSONNode node = new JSONObject();
            node["f"] = vfxBox.IndexFilter.ToJson();
            node["w"] = JSONNumber.RoundBeat(vfxBox.BeatDistribution);
            node["d"] = vfxBox.BeatDistributionType;
            node["s"] = vfxBox.VfxDistribution;
            node["t"] = vfxBox.VfxDistributionType;
            node["b"] = vfxBox.VfxAffectFirst;
            node["i"] = vfxBox.Easing;

            node["l"] = new JSONArray();
            foreach (var data in vfxBox.Events)
            {
                node["l"].Add(floatFxEvents.IndexOf(data));
            }

            return node;
        }

        // Fix: Add overload for node editor serialization - uses the box's own events for serialization
        public static JSONNode ToJson(BaseVfxEventEventBox vfxBox)
        {
            // Handle null or empty box
            if (vfxBox == null)
            {
                Debug.LogWarning("[V3VfxEventEventBox] Attempted to serialize null box");
                return new JSONObject();
            }

            var floatFxEvents = vfxBox.Events.ToList();
            var result = ToJson(vfxBox, floatFxEvents);
            return result;
        }
    }
}
