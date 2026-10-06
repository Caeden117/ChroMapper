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
        public static BaseVfxEventEventBoxGroup GetFromJson(
            JSONNode node,
            IList<BaseIndexFilter> indexFilters,
            IList<V4CommonData.FxEventBox> fxEventBoxesCommonData,
            IList<V4CommonData.FloatFxEvent> floatFxEventsCommonData)
        {
            var group = new BaseVfxEventEventBoxGroup();

            group.JsonTime = node["b"].AsFloat;
            group.ID = node["g"].AsInt;

            group.Boxes = node["e"]
                .AsArray.Linq.Select((x, i) =>
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

                    box.Events = boxNode["l"]
                        .AsArray.Linq.Select(y =>
                        {
                            var eventNode = y.Value;

                            var evt = new BaseFxEventFloat();
                            evt.RelativeJsonTime = eventNode["b"].AsFloat;

                            evt.EventBoxData = box;
                            evt.EventBoxGroupData = group;
                            evt.BoxIndex = i;
                            evt.JsonTime = group.JsonTime + evt.RelativeJsonTime;

                            var eventIndex = eventNode["i"].AsInt;
                            var commonEventData = floatFxEventsCommonData[eventIndex];

                            evt.Value = commonEventData.Value;
                            evt.UsePrevious = commonEventData.TransitionType;
                            evt.Easing = commonEventData.Easing;

                            return evt;
                        })
                        .ToArray();

                    return box;
                })
                .ToList();

            group.CustomData = node["customData"];

            // Remove invalid same-lane/same-beat nodes once the loaded group can produce actionable beat diagnostics.
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        public static JSONNode ToJson(
            BaseVfxEventEventBoxGroup group,
            IList<V4CommonData.IndexFilter> indexFiltersCommonData,
            IList<V4CommonData.FxEventBox> fxEventBoxesCommonData,
            IList<V4CommonData.FloatFxEvent> floatFxEventsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(group.JsonTime);
            node["g"] = group.ID;
            node["t"] = 4;

            var boxArray = new JSONArray();

            foreach (var boxEvent in group.Boxes)
            {
                var boxNode = new JSONObject();
                boxNode["f"] =
                    indexFiltersCommonData.IndexOf(V4CommonData.IndexFilter.FromBaseIndexFilter(boxEvent.IndexFilter));
                boxNode["e"] =
                    fxEventBoxesCommonData.IndexOf(
                        V4CommonData.FxEventBox.FromBaseFxEventBox(boxEvent));

                var eventArray = new JSONArray();

                foreach (var floatEvent in boxEvent.Events)
                {
                    var eventNode = new JSONObject();
                    eventNode["b"] = JSONNumber.RoundBeat(floatEvent.RelativeJsonTime);
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
