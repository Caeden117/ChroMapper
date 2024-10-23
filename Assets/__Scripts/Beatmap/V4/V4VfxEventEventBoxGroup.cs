using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V4
{
    public static class V4VfxEventEventBoxGroup
    {
        public static BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> GetFromJson(JSONNode node,
            IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.FxEventBox> fxEventBoxesCommonData,
            IList<V4CommonData.FloatFxEvent> floatFxEventsCommonData)
        {
            var vfxGroup = new BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>();
            
            vfxGroup.JsonTime = node["b"].AsFloat;
            vfxGroup.ID = node["g"].AsInt;

            vfxGroup.Events = node["e"].AsArray.Linq.Select(x =>
                {
                    var boxNode = x.Value;

                    var box = new BaseVfxEventEventBox();
                    
                    var filterIndex = boxNode["f"].AsInt;
                    box.IndexFilter = (BaseIndexFilter)indexFilters[filterIndex].Clone();

                    var boxIndex = boxNode["e"].AsInt;
                    var boxCommonData = fxEventBoxesCommonData[boxIndex];
                    
                    box.BeatDistribution = boxCommonData.BeatDistribution;
                    box.BeatDistributionType = boxCommonData.BeatDistributionType;
                    box.VfxDistribution = boxCommonData.FxDistribution;
                    box.VfxDistributionType = boxCommonData.FxDistributionType;
                    box.VfxAffectFirst = boxCommonData.FxAffectFirst;
                    box.Easing = boxCommonData.Easing;
                    
                    // TODO: Well... Base model needs be redone
                    //box.VfxData = 
                    
                    return box;
                })
                .ToList();

            vfxGroup.CustomData = node["customData"];

            return vfxGroup;
        }
        public static JSONNode ToJson<T>(BaseVfxEventEventBoxGroup<T> vfxGroup) where T : BaseVfxEventEventBox
        {
            JSONNode node = new JSONObject();
            // node["b"] = vfxGroup.JsonTime;
            // node["g"] = vfxGroup.ID;
            // node["t"] = vfxGroup.Type;
            // var ary = new JSONArray();
            // foreach (var k in vfxGroup.Events) ary.Add(V3VfxEventEventBox.ToJson(k));
            // node["e"] = ary;
            // vfxGroup.CustomData = vfxGroup.SaveCustom();
            // if (!vfxGroup.CustomData.Children.Any()) return node;
            // node["customData"] = vfxGroup.CustomData;
            return node;
        }
    }
}
