using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Comparers;
using UnityEngine;

namespace Beatmap.Shared
{
    // Chroma keeps one colorizer per event type. Gradients and boost refreshes can therefore recolor
    // a light whose own most recent event selected a different ID.
    public sealed class BasicEventChromaColorTimeline
    {
        private readonly List<Entry> entries = new();
        private readonly Dictionary<BaseEvent, Entry> eventEntries = new();
        private BaseDifficulty map;
        private bool emulateColors;
        private int nextOrder;

        public int Version { get; private set; }

        public void Initialize(BaseDifficulty difficulty, bool emulateChromaColors)
        {
            map = difficulty;
            emulateColors = emulateChromaColors;
            entries.Clear();
            eventEntries.Clear();
            nextOrder = 0;
            Version++;
        }

        public void Insert(BaseEvent data)
        {
            var entry = new Entry(data, nextOrder++);
            var index = FindInsertionIndex(entry);
            entries.Insert(index, entry);
            eventEntries.Add(data, entry);
            RebuildFrom(index);
            Version++;
        }

        public void Remove(BaseEvent data)
        {
            var entry = eventEntries[data];
            var index = entries.IndexOf(entry);
            entries.RemoveAt(index);
            eventEntries.Remove(data);
            RebuildFrom(index);
            Version++;
        }

        public Color? GetSourceColor(BaseEvent data) => eventEntries.TryGetValue(data, out var entry)
            ? entry.State.TypeColor.Evaluate(entry.Time)
            : null;

        public Color? GetColorData(BaseEvent data) => eventEntries.TryGetValue(data, out var entry)
            ? entry.ColorData
            : null;

        public Frame GetFrame(float time, (float Time, int FileOrder) boostRefresh)
        {
            var boostRefreshTime = boostRefresh.Time;
            var index = FindAtOrBefore(time);
            if (index < 0)
                return new Frame(boostRefreshTime, null, false);

            var state = entries[index].State;
            var refreshTime = state.LastGradientRefreshTime;
            var refreshColor = state.LastGradientRefreshColor;
            var activeGradient = false;
            if (state.Gradient.HasValue)
            {
                var gradient = state.Gradient.Value;
                refreshTime = Mathf.Min(time, gradient.EndTime);
                refreshColor = gradient.Evaluate(refreshTime);
                activeGradient = time <= gradient.EndTime;
            }

            if (boostRefreshTime > refreshTime)
            {
                var boostIndex = FindAtOrBefore(boostRefreshTime, boostRefresh.FileOrder);
                refreshTime = boostRefreshTime;
                refreshColor = boostIndex >= 0
                    ? entries[boostIndex].State.TypeColor.Evaluate(boostRefreshTime)
                    : null;
                activeGradient = false;
            }

            return new Frame(refreshTime, refreshColor, activeGradient);
        }

        private int FindInsertionIndex(Entry entry)
        {
            var lower = 0;
            var upper = entries.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) / 2);
                var comparison = entries[middle].CompareTo(entry);
                if (comparison <= 0)
                    lower = middle + 1;

                else
                    upper = middle;

            }

            return lower;
        }

        private int FindAtOrBefore(float time, int fileOrder = int.MaxValue)
        {
            var lower = 0;
            var upper = entries.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) / 2);
                if (entries[middle].Time < time
                    || (entries[middle].Time == time && entries[middle].FileOrder <= fileOrder))
                {
                    lower = middle + 1;
                }

                else
                    upper = middle;

            }

            return lower - 1;
        }

        private void RebuildFrom(int index)
        {
            var state = index > 0
                ? entries[index - 1].State
                : new State { LastGradientRefreshTime = float.NegativeInfinity };
            for (var i = index; i < entries.Count; i++)
            {
                var entry = entries[i];
                var data = entry.Source;
                if (state.Gradient.HasValue)
                {
                    var previousGradient = state.Gradient.Value;
                    state.LastGradientRefreshTime = Mathf.Min(entry.Time, previousGradient.EndTime);
                    state.LastGradientRefreshColor = previousGradient.Evaluate(state.LastGradientRefreshTime);
                }

                if (emulateColors && data.Value >= ColourManager.RgbintOffset)
                    state.LegacyColor = ColourManager.ColourFromInt(data.Value);

                else if (emulateColors && data.Value == ColourManager.RGBReset)
                    state.LegacyColor = null;

                entry.ColorData = emulateColors
                    ? data.CustomColor ?? state.LegacyColor
                    : null;
                if (entry.ColorData.HasValue)
                {
                    state.TypeColor = new ColorDefinition(entry.ColorData);
                    state.Gradient = null;
                }

                else if (emulateColors && data.CustomLightGradient != null)
                {
                    var definition = new ColorDefinition(
                        entry.Time,
                        GetGradientEndTime(data),
                        data.CustomLightGradient);
                    state.Gradient = definition.IsGradient
                        ? definition
                        : null;
                    state.TypeColor = definition;
                }

                else if (!state.Gradient.HasValue || entry.Time > state.Gradient.Value.EndTime)
                {
                    state.Gradient = null;
                    state.TypeColor = default;
                }

                entry.State = state;
            }
        }

        private float GetGradientEndTime(BaseEvent data)
        {
            // Chroma freezes the duration in seconds using the BPM at the callback, rather than
            // integrating later BPM changes across the authored duration.
            var bpmEvents = map.BpmEvents;
            var lower = 0;
            var upper = bpmEvents.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) / 2);
                if (bpmEvents[middle].JsonTime <= data.JsonTime)
                    lower = middle + 1;

                else
                    upper = middle;

            }

            var bpm = lower > 0
                ? bpmEvents[lower - 1].Bpm
                : map.SongBpm.Value;
            return data.SongBpmTime + (data.CustomLightGradient.Duration * map.SongBpm.Value / bpm);
        }

        public readonly struct Frame
        {
            public readonly float RefreshTime;
            public readonly Color? Color;
            public readonly bool ActiveGradient;

            public Frame(float refreshTime, Color? color, bool activeGradient)
            {
                RefreshTime = refreshTime;
                Color = color;
                ActiveGradient = activeGradient;
            }
        }

        private sealed class Entry
        {
            public readonly BaseEvent Source;
            public readonly float Time;
            private readonly float jsonTime;
            public readonly int FileOrder;
            private readonly int insertionOrder;
            public Color? ColorData;
            public State State;

            public Entry(BaseEvent source, int order)
            {
                Source = source;
                Time = source.SongBpmTime;
                jsonTime = source.JsonTime;
                FileOrder = source.FileOrder;
                insertionOrder = order;
            }

            public int CompareTo(Entry other)
            {
                var comparison = EventOrderComparer.CompareKeys(jsonTime, FileOrder, other.jsonTime, other.FileOrder);
                return comparison != 0
                    ? comparison
                    : insertionOrder.CompareTo(other.insertionOrder);
            }
        }

        private struct State
        {
            public Color? LegacyColor;
            public ColorDefinition TypeColor;
            public ColorDefinition? Gradient;
            public float LastGradientRefreshTime;
            public Color? LastGradientRefreshColor;
        }

        private readonly struct ColorDefinition
        {
            private readonly Color? constant;
            private readonly bool gradient;
            private readonly float startTime;
            public readonly float EndTime;
            private readonly Color startColor;
            private readonly Color endColor;
            private readonly Func<float, float> easing;

            public bool IsGradient => gradient;

            public ColorDefinition(Color? color)
            {
                constant = color;
                gradient = false;
                startTime = EndTime = 0f;
                startColor = endColor = default;
                easing = null;
            }

            public ColorDefinition(float start, float end, ChromaLightGradient data)
            {
                // Chroma finishes nonpositive durations after the callback without registering
                // gradient ticks that would refresh other IDs.
                gradient = end > start;
                constant = gradient
                    ? null
                    : data.EndColor;
                startTime = start;
                EndTime = end;
                startColor = data.StartColor;
                endColor = data.EndColor;
                easing = Easing.HeckNamed(data.EasingType);
            }

            public Color? Evaluate(float time) => gradient
                ? Color.LerpUnclamped(startColor, endColor, easing(Mathf.InverseLerp(startTime, EndTime, time)))
                : constant;
        }
    }
}
