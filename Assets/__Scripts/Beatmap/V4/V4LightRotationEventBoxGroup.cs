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
        public static BaseLightRotationEventBoxGroup GetFromJson(
            JSONNode node,
            IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.LightRotationEventBox> lightRotationEventBoxesCommonData,
            IList<V4CommonData.LightRotationEvent> lightRotationEventsCommonData)
        {
            var group = new BaseLightRotationEventBoxGroup();

            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            var boxEvents = node["e"].AsArray;
            group.Boxes = boxEvents
                .Linq.Select((x, i) =>
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
                    box.Axis = commonBoxData.Axis;
                    box.Flip = commonBoxData.Flip;

                    box.Events = boxNode["l"]
                        .AsArray.Linq.Select(x =>
                        {
                            var eventNode = x.Value;

                            var evt = new BaseLightRotationBase();
                            evt.RelativeJsonTime = eventNode["b"].AsFloat;

                            evt.EventBoxData = box;
                            evt.EventBoxGroupData = group;
                            evt.BoxIndex = i;
                            evt.JsonTime = group.JsonTime + evt.RelativeJsonTime;

                            var eventIndex = eventNode["i"].AsInt;
                            var commonEventData = lightRotationEventsCommonData[eventIndex];

                            evt.Rotation = commonEventData.Rotation;
                            evt.UsePrevious = commonEventData.TransitionType;
                            evt.Direction = commonEventData.Direction;
                            evt.Loop = commonEventData.Loop;
                            evt.EaseType = commonEventData.Easing;

                            return evt;
                        })
                        .ToArray();

                    return box;
                })
                .ToList();

            // Remove invalid same-lane/same-beat nodes once the loaded group can produce actionable beat diagnostics.
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        public static JSONNode ToJson(
            BaseLightRotationEventBoxGroup group,
            IList<V4CommonData.IndexFilter> indexFiltersCommonData,
            IList<V4CommonData.LightRotationEventBox> lightRotationEventBoxesCommonData,
            IList<V4CommonData.LightRotationEvent> lightRotationEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(group.JsonTime);
            node["g"] = group.ID;
            node["t"] = 2;

            var boxArray = new JSONArray();

            foreach (var boxEvent in group.Boxes)
            {
                var boxNode = new JSONObject();
                boxNode["f"] =
                    indexFiltersCommonData.IndexOf(V4CommonData.IndexFilter.FromBaseIndexFilter(boxEvent.IndexFilter));
                boxNode["e"] =
                    lightRotationEventBoxesCommonData.IndexOf(
                        V4CommonData.LightRotationEventBox.FromBaseLightRotationEventBox(boxEvent));

                var eventArray = new JSONArray();

                foreach (var evt in boxEvent.Events)
                {
                    var eventNode = new JSONObject();
                    eventNode["b"] = JSONNumber.RoundBeat(evt.RelativeJsonTime);
                    eventNode["i"] =
                        lightRotationEventsCommonData.IndexOf(
                            V4CommonData.LightRotationEvent.FromBaseLightRotationEvent(evt));

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
