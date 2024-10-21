using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V4
{
    public static class V4LightRotationEventBoxGroup
    {
        public static BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> GetFromJson(JSONNode node, IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.LightRotationEventBox> lightRotationEventBoxesCommonData, 
            IList<V4CommonData.LightRotationEvent> lightRotationEventsCommonData)
        {
            var group = new BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>();
            
            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            var boxEvents = node["e"].AsArray;
            group.Events = boxEvents.Linq.Select(x =>
            {
                var boxNode = x.Value;

                var box = new BaseLightRotationEventBox();
                
                var filterIndex = boxNode["f"].AsInt;
                box.IndexFilter = (BaseIndexFilter)indexFilters[filterIndex].Clone();

                var boxIndex = boxNode["e"].AsInt;
                var commonBoxData = lightRotationEventBoxesCommonData[boxIndex];
                box.BeatDistribution = commonBoxData.BeatDistribution;
                box.BeatDistributionType = commonBoxData.BeatDistributionType;
                box.RotationDistribution = commonBoxData.RotationDistribution;
                box.RotationDistributionType = commonBoxData.RotationDistributionType;
                box.RotationAffectFirst = commonBoxData.RotationAffectFirst;
                box.Easing = commonBoxData.Easing;

                box.Events = node["l"].AsArray.Linq.Select(
                    x =>
                    {
                        var eventNode = x.Value;
                        
                        var evt = new BaseLightRotationBase();
                        evt.JsonTime = eventNode["b"].AsFloat;

                        var eventIndex = eventNode["i"].AsInt;
                        var commonEventData = lightRotationEventsCommonData[eventIndex];

                        evt.Rotation = commonEventData.Rotation;
                        evt.UsePrevious = commonEventData.TransitionType;
                        evt.Direction = commonEventData.Direction;
                        evt.Loop = commonEventData.Loop;
                        evt.EaseType = commonEventData.Easing;
                        
                        return evt;
                    }).ToArray();

                return box;
            }).ToList();

            return group;
        }

        public static JSONNode ToJson<T>(BaseLightRotationEventBoxGroup<T> group) where T : BaseLightRotationEventBox
        {
            JSONNode node = new JSONObject();
            // node["b"] = group.JsonTime;
            // node["g"] = group.ID;
            // var ary = new JSONArray();
            // foreach (var k in group.Events) ary.Add(V3LightRotationEventBox.ToJson(k));
            // node["e"] = ary;
            // group.CustomData = group.SaveCustom();
            // if (!group.CustomData.Children.Any()) return node;
            // node["customData"] = group.CustomData;
            return node;
        }
    }
}
