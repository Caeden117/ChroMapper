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

                box.Events = node["t"].AsArray.Linq.Select(
                    x =>
                    {
                        var eventNode = x.Value;
                        
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

        public static JSONNode ToJson<T>(BaseLightTranslationEventBoxGroup<T> group) where T : BaseLightTranslationEventBox
        {
            JSONNode node = new JSONObject();
            // node["b"] = group.JsonTime;
            // node["g"] = group.ID;
            // var ary = new JSONArray();
            // foreach (var k in group.Events) ary.Add(V3LightTranslationEventBox.ToJson(k));
            // node["e"] = ary;
            // group.CustomData = group.SaveCustom();
            // if (!group.CustomData.Children.Any()) return node;
            // node["customData"] = group.CustomData;
            return node;
        }
    }
}
