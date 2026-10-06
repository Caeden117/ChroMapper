using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V4;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;
using Debug = UnityEngine.Debug;

namespace Beatmap.V3
{
    public static class V3VfxEventEventBoxGroup
    {
        public static BaseVfxEventEventBoxGroup GetFromJson(JSONNode node, IList<BaseFxEventFloat> floatFxEvents)
        {
            var group = new BaseVfxEventEventBoxGroup
            {
                JsonTime = node["b"].AsFloat,
                ID = node["g"].AsInt,
                Type = node["t"].AsInt,
                CustomData = node["customData"]
            };
            group.Boxes = BaseItem
                .GetRequiredNode(node, "e")
                .AsArray.Linq
                .Select((x, i) =>
                {
                    var box = V3VfxEventEventBox.GetFromJson(x.Value, floatFxEvents);
                    foreach (var evt in box.Events)
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        evt.BoxIndex = i;
                        evt.JsonTime = group.JsonTime;
                    }

                    return box;
                })
                .ToList();

            // Remove invalid same-lane/same-beat nodes once the loaded group can produce actionable beat diagnostics.
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        public static JSONNode ToJson(
            BaseVfxEventEventBoxGroup vfxGroup,
            IList<BaseFxEventFloat> floatFxEvents)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(vfxGroup.JsonTime);
            node["g"] = vfxGroup.ID;
            node["t"] = vfxGroup.Type;
            var ary = new JSONArray();
            foreach (var k in vfxGroup.Boxes) ary.Add(V3VfxEventEventBox.ToJson(k, floatFxEvents));
            node["e"] = ary;
            vfxGroup.CustomData = vfxGroup.SaveCustom();
            if (!vfxGroup.CustomData.Children.Any()) return node;
            node["customData"] = vfxGroup.CustomData;
            return node;
        }

        // Fix: Add overload for node editor serialization - extracts float FX events from the group automatically
        public static JSONNode ToJson(BaseVfxEventEventBoxGroup vfxGroup)
        {
            // Handle null or empty group
            if (vfxGroup == null)
            {
                Debug.LogWarning("[V3VfxEventEventBoxGroup] Attempted to serialize null group");
                return new JSONObject();
            }

            // Use actual event instances from boxes to ensure IndexOf works correctly
            var floatFxEvents = vfxGroup.Boxes
                .SelectMany(box => box.Events)
                .Distinct()
                .ToList();
            var result = ToJson(vfxGroup, floatFxEvents);
            return result;
        }
    }
}
