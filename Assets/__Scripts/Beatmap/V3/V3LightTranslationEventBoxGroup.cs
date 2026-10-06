using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightTranslationEventBoxGroup
    {
        public static BaseLightTranslationEventBoxGroup GetFromJson(JSONNode node)
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = node["b"].AsFloat, ID = node["g"].AsInt, CustomData = node["customData"]
            };
            group.Boxes = BaseItem
                .GetRequiredNode(node, "e")
                .AsArray.Linq
                .Select((x, i) =>
                {
                    var box = V3LightTranslationEventBox.GetFromJson(x);
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

        public static JSONNode ToJson(BaseLightTranslationEventBoxGroup box)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(box.JsonTime);
            node["g"] = box.ID;
            var ary = new JSONArray();
            foreach (var k in box.Boxes) ary.Add(k.ToJson());
            node["e"] = ary;
            box.CustomData = box.SaveCustom();
            if (!box.CustomData.Children.Any()) return node;
            node["customData"] = box.CustomData;
            return node;
        }
    }
}
