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


                    box.FloatFxEvents = boxNode["l"].AsArray.Linq.Select(
                        y =>
                        {
                            var eventNode = y.Value;
                        
                            var evt = new FloatFxEventBase();
                            evt.JsonTime = eventNode["b"].AsFloat;

                            var eventIndex = eventNode["i"].AsInt;
                            var commonEventData = floatFxEventsCommonData[eventIndex];
                        
                            evt.Value = commonEventData.Value;
                            evt.UsePreviousEventValue = commonEventData.TransitionType;
                            evt.Easing = commonEventData.Easing;
                        
                            return evt;
                        }).ToList();
                    
                    return box;
                })
                .ToList();

            vfxGroup.CustomData = node["customData"];

            return vfxGroup;
        }
        public static JSONNode ToJson(BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> group,
            IList<V4CommonData.IndexFilter> indexFiltersCommonData,
            IList<V4CommonData.FxEventBox > fxEventBoxesCommonData,
            IList<V4CommonData.FloatFxEvent> floatFxEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = group.JsonTime;
            node["g"] = group.ID;
            node["t"] = 4;
            
            var boxArray = new JSONArray();

            foreach (var boxEvent in group.Events)
            {
                var boxNode = new JSONObject();
                boxNode["f"] =
                    indexFiltersCommonData.IndexOf(V4CommonData.IndexFilter.FromBaseIndexFilter(boxEvent.IndexFilter));
                boxNode["e"] =
                    fxEventBoxesCommonData.IndexOf(
                        V4CommonData.FxEventBox.FromBaseFxEventBox(boxEvent));

                var eventArray = new JSONArray();

                foreach (var floatEvent in boxEvent.FloatFxEvents)
                {
                    var eventNode = new JSONObject();
                    eventNode["b"] = floatEvent.JsonTime;
                    eventNode["i"] =
                        floatFxEventsCommonData.IndexOf(V4CommonData.FloatFxEvent.FromFloatFxEventBase(floatEvent));
                    
                    eventArray.Add(eventNode);
                }

                boxNode["l"] = eventArray;
                
                boxArray.Add(boxNode);
            }

            node["e"] = boxArray;

            return node;
        }
    }
}
