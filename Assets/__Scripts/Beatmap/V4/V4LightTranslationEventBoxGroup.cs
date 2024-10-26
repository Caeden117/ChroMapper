using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V4
{
    public static class V4LightTranslationEventBoxGroup
    {
        public static BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> GetFromJson(JSONNode node, IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.LightTranslationEventBox> lightTranslationEventBoxesCommonData, 
            IList<V4CommonData.LightTranslationEvent> lightTranslationEventsCommonData)
        {
            var group = new BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>();
            
            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            var boxEvents = node["e"].AsArray;
            group.Events = boxEvents.Linq.Select(x =>
            {
                var boxNode = x.Value;

                var box = new BaseLightTranslationEventBox();
                
                var filterIndex = boxNode["f"].AsInt;
                box.IndexFilter = (BaseIndexFilter)indexFilters[filterIndex].Clone();

                var boxIndex = boxNode["e"].AsInt;
                var commonBoxData = lightTranslationEventBoxesCommonData[boxIndex];
                box.BeatDistribution = commonBoxData.BeatDistribution;
                box.BeatDistributionType = commonBoxData.BeatDistributionType;
                box.TranslationDistribution = commonBoxData.TranslationDistribution;
                box.TranslationDistributionType = commonBoxData.TranslationDistributionType;
                box.TranslationAffectFirst = commonBoxData.TranslationAffectFirst;
                box.Easing = commonBoxData.Easing;
                box.Flip = commonBoxData.Flip;

                box.Events = boxNode["l"].AsArray.Linq.Select(
                    y =>
                    {
                        var eventNode = y.Value;
                        
                        var evt = new BaseLightTranslationBase();
                        evt.JsonTime = eventNode["b"].AsFloat;

                        var eventIndex = eventNode["i"].AsInt;
                        var commonEventData = lightTranslationEventsCommonData[eventIndex];

                        evt.Translation = commonEventData.Translation;
                        evt.UsePrevious = commonEventData.TransitionType;
                        evt.EaseType = commonEventData.Easing;
                        
                        return evt;
                    }).ToArray();

                return box;
            }).ToList();

            return group;
        }

        public static JSONNode ToJson(BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> group,
            IList<V4CommonData.IndexFilter> indexFiltersCommonData,
            IList<V4CommonData.LightTranslationEventBox> lightTranslationEventBoxesCommonData,
            IList<V4CommonData.LightTranslationEvent> lightTranslationEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = group.JsonTime;
            node["g"] = group.ID;
            node["t"] = 3;
            
            var boxArray = new JSONArray();

            foreach (var boxEvent in group.Events)
            {
                var boxNode = new JSONObject();
                boxNode["f"] =
                    indexFiltersCommonData.IndexOf(V4CommonData.IndexFilter.FromBaseIndexFilter(boxEvent.IndexFilter));
                boxNode["e"] =
                    lightTranslationEventBoxesCommonData.IndexOf(
                        V4CommonData.LightTranslationEventBox.FromBaseLightTranslationEventBox(boxEvent));

                var eventArray = new JSONArray();

                foreach (var evt in boxEvent.Events)
                {
                    var eventNode = new JSONObject();
                    eventNode["b"] = evt.JsonTime;
                    eventNode["i"] =
                        lightTranslationEventsCommonData.IndexOf(V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent(evt));
                    
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
