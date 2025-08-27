using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V4;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3VfxEventEventBoxGroup
    {
        public static BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> GetFromJson(JSONNode node, IList<FloatFxEventBase> floatFxEvents)
        {
            var vfxGroup = new BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>();
            
            vfxGroup.JsonTime = node["b"].AsFloat;
            vfxGroup.ID = node["g"].AsInt;
            vfxGroup.Type = node["t"].AsInt;
            vfxGroup.Events = new List<BaseVfxEventEventBox>(BaseItem.GetRequiredNode(node, "e").AsArray.Linq
                .Select(x => V3VfxEventEventBox.GetFromJson(x.Value, floatFxEvents)).ToList());
            vfxGroup.CustomData = node["customData"];

            return vfxGroup;
        }
        public static JSONNode ToJson(BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> vfxGroup,
            IList<FloatFxEventBase> floatFxEvents)
        {
            JSONNode node = new JSONObject();
            node["b"] = vfxGroup.JsonTime;
            node["g"] = vfxGroup.ID;
            node["t"] = vfxGroup.Type;
            var ary = new JSONArray();
            foreach (var k in vfxGroup.Events) ary.Add(V3VfxEventEventBox.ToJson(k, floatFxEvents));
            node["e"] = ary;
            vfxGroup.CustomData = vfxGroup.SaveCustom();
            if (!vfxGroup.CustomData.Children.Any()) return node;
            node["customData"] = vfxGroup.CustomData;
            return node;
        }
    }
}
