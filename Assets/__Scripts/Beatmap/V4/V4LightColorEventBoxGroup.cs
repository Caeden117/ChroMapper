using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V4
{
    public static class V4LightColorEventBoxGroup
    {
        public static BaseLightColorEventBoxGroup<BaseLightColorEventBox> GetFromJson(JSONNode node, IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.LightColorEventBox> lightColorEventBoxesCommonData, 
            IList<V4CommonData.LightColorEvent> lightColorEventsCommonData)
        {
            var group = new BaseLightColorEventBoxGroup<BaseLightColorEventBox>();
            
            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            var boxEvents = node["e"].AsArray;
            group.Events = boxEvents.Linq.Select(x =>
            {
                var boxNode = x.Value;

                var box = new BaseLightColorEventBox();
                
                var filterIndex = boxNode["f"].AsInt;
                box.IndexFilter = (BaseIndexFilter)indexFilters[filterIndex].Clone();

                var boxIndex = boxNode["e"].AsInt;
                var commonBoxData = lightColorEventBoxesCommonData[boxIndex];
                box.BeatDistribution = commonBoxData.BeatDistribution;
                box.BeatDistributionType = commonBoxData.BeatDistributionType;
                box.BrightnessDistribution = commonBoxData.BrightnessDistribution;
                box.BrightnessDistributionType = commonBoxData.BrightnessDistributionType;
                box.BrightnessAffectFirst = commonBoxData.BrightnessAffectFirst;
                box.Easing = commonBoxData.Easing;

                box.Events = node["l"].AsArray.Linq.Select(
                    x =>
                    {
                        var eventNode = x.Value;
                        
                        var evt = new BaseLightColorBase();
                        evt.JsonTime = eventNode["b"].AsFloat;

                        var eventIndex = eventNode["i"].AsInt;
                        var commonEventData = lightColorEventsCommonData[eventIndex];

                        evt.Brightness = commonEventData.Brightness;
                        evt.TransitionType = commonEventData.TransitionType;
                        evt.Frequency = commonEventData.Frequency;
                        evt.StrobeBrightness = commonEventData.StrobeBrightness;
                        evt.StrobeFade = commonEventData.StrobeFade;
                        evt.Easing = commonEventData.Easing;
                        
                        return evt;
                    }).ToArray();

                return box;
            }).ToList();

            return group;
        }

        public static JSONNode ToJson<T>(BaseLightColorEventBoxGroup<T> group) where T : BaseLightColorEventBox
        {
            JSONNode node = new JSONObject();
            // node["b"] = group.JsonTime;
            // node["g"] = group.ID;
            // var ary = new JSONArray();
            // foreach (var k in group.Events) ary.Add(V3LightColorEventBox.ToJson(k));
            // node["e"] = ary;
            // group.CustomData = group.SaveCustom();
            // if (!group.CustomData.Children.Any()) return node;
            // node["customData"] = group.CustomData;
            return node;
        }
    }
}
