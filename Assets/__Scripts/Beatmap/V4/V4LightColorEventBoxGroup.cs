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
        public static BaseLightColorEventBoxGroup GetFromJson(
            JSONNode node,
            IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.LightColorEventBox> lightColorEventBoxesCommonData,
            IList<V4CommonData.LightColorEvent> lightColorEventsCommonData)
        {
            var group = new BaseLightColorEventBoxGroup();

            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            var boxEvents = node["e"].AsArray;
            group.Boxes = boxEvents
                .Linq.Select((x, i) =>
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

                    box.Events = boxNode["l"]
                        .AsArray.Linq.Select(x =>
                        {
                            var eventNode = x.Value;

                            var evt = new BaseLightColorBase();
                            evt.RelativeJsonTime = eventNode["b"].AsFloat;

                            evt.EventBoxData = box;
                            evt.EventBoxGroupData = group;
                            evt.BoxIndex = i;
                            evt.JsonTime = group.JsonTime + evt.RelativeJsonTime;

                            var eventIndex = eventNode["i"].AsInt;
                            var commonEventData = lightColorEventsCommonData[eventIndex];

                            evt.Color = commonEventData.Color;
                            evt.Brightness = commonEventData.Brightness;
                            evt.Easing = commonEventData.Easing;
                            evt.UsePrevious = commonEventData.UsePrevious;
                            evt.Frequency = commonEventData.Frequency;
                            evt.StrobeBrightness = commonEventData.StrobeBrightness;
                            evt.StrobeFade = commonEventData.StrobeFade;
                            evt.CustomData = eventNode["customData"];
                            evt.RefreshCustom();

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
            BaseLightColorEventBoxGroup group,
            IList<V4CommonData.IndexFilter> indexFiltersCommonData,
            IList<V4CommonData.LightColorEventBox> lightColorEventBoxesCommonData,
            IList<V4CommonData.LightColorEvent> lightColorEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(group.JsonTime);
            node["g"] = group.ID;
            node["t"] = 1;

            var boxArray = new JSONArray();

            foreach (var boxEvent in group.Boxes)
            {
                var boxNode = new JSONObject();
                boxNode["f"] =
                    indexFiltersCommonData.IndexOf(V4CommonData.IndexFilter.FromBaseIndexFilter(boxEvent.IndexFilter));
                boxNode["e"] =
                    lightColorEventBoxesCommonData.IndexOf(
                        V4CommonData.LightColorEventBox.FromBaseLightColorEventBox(boxEvent));

                var eventArray = new JSONArray();

                foreach (var evt in boxEvent.Events)
                {
                    var eventNode = new JSONObject();
                    eventNode["b"] = JSONNumber.RoundBeat(evt.RelativeJsonTime);
                    eventNode["i"] =
                        lightColorEventsCommonData.IndexOf(V4CommonData.LightColorEvent.FromBaseLightColorEvent(evt));
                    evt.CustomData = evt.SaveCustom();
                    if (evt.CustomData.Children.Any())
                        eventNode["customData"] = evt.CustomData;

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
