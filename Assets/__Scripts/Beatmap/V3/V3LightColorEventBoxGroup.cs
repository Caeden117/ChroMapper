using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightColorEventBoxGroup
    {
        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> GetFromJson(JSONNode node)
        {
            var group = new BaseLightColorEventBoxGroup<BaseLightColorEventBox>();
            
            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;
            group.Events = new List<BaseLightColorEventBox>(BaseItem.GetRequiredNode(node, "e").AsArray.Linq
                .Select(x => V3LightColorEventBox.GetFromJson(x.Value)).ToList());
            group.CustomData = node["customData"];

            return group;
        }

        public static JSONNode ToJson<T>(BaseLightColorEventBoxGroup<T> group) where T : BaseLightColorEventBox
        {
            JSONNode node = new JSONObject();
            node["b"] = group.JsonTime;
            node["g"] = group.ID;
            var ary = new JSONArray();
            foreach (var k in group.Events) ary.Add(V3LightColorEventBox.ToJson(k));
            node["e"] = ary;
            group.CustomData = group.SaveCustom();
            if (!group.CustomData.Children.Any()) return node;
            node["customData"] = group.CustomData;
            return node;
        }
    }
}
