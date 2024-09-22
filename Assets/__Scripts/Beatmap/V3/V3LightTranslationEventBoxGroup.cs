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
        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> GetFromJson(JSONNode node)
        {
            var group = new BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>();
            
            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;
            group.Events = new List<BaseLightTranslationEventBox>(BaseItem.GetRequiredNode(node, "e").AsArray.Linq
                .Select(x => V3LightTranslationEventBox.GetFromJson(x)).ToList());
            group.CustomData = node["customData"];

            return group;
        }

        public static JSONNode ToJson<T>(BaseLightTranslationEventBoxGroup<T> box) where T : BaseLightTranslationEventBox
        {
            JSONNode node = new JSONObject();
            node["b"] = box.JsonTime;
            node["g"] = box.ID;
            var ary = new JSONArray();
            foreach (var k in box.Events) ary.Add(k.ToJson());
            node["e"] = ary;
            box.CustomData = box.SaveCustom();
            if (!box.CustomData.Children.Any()) return node;
            node["customData"] = box.CustomData;
            return node;
        }
    }
}
