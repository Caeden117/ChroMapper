using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
///     Indexes authored color-boost events by JSON time for allocation-free palette-state queries.
/// </summary>
/// <remarks>
///     Basic-event appearance and preview updates need the last boost at or before a time and the next boost after a
///     mutation. The contiguous timestamp list makes both queries <c>O(log(N))</c> without depending on framework
///     range-view behavior or allocating a view. Mutations are infrequent editor operations and cost <c>O(N)</c> for
///     list insertion/removal. This could easily be adjusted to be chunked lists if we notice that placing / editing
///     boost events in maps with lots of boost events takes noticeable processing time,
///     but these seem several orders of magnitude less common than EG light events.
/// </remarks>
internal sealed class ColorBoostEventIndex
{
    // One event per timestamp is authoritative; the timestamp list supports allocation-free predecessor searches.
    private readonly List<float> eventTimes = new();
    private readonly Dictionary<float, BaseEvent> eventsByTime = new();

    // Coalesce boost mutations into the exact light-event interval whose palette can have changed.
    private float appearanceStartTime = float.PositiveInfinity;
    private float appearanceEndTime = float.NegativeInfinity;

    // Rebuild the data-only index when a map's basic event collection is loaded.
    public void Load(IEnumerable<BaseEvent> events)
    {
        eventTimes.Clear();
        eventsByTime.Clear();

        foreach (var evt in events)
        {
            // Filter while rebuilding so map load does not allocate a temporary boost-event list.
            if (!evt.IsColorBoostEvent())
            {
                continue;
            }

            Add(evt);
        }
    }

    // Insert at the binary-search insertion point so time queries remain logarithmic.
    public void Add(BaseEvent evt)
    {
        var search = BinarySearchTime(evt.JsonTime);
        if (search < 0)
        {
            eventTimes.Insert(~search, evt.JsonTime);
        }

        eventsByTime[evt.JsonTime] = evt;
    }

    // Only remove the currently indexed instance so replacing a boost cannot remove its replacement.
    public void Remove(BaseEvent evt)
    {
        if (!eventsByTime.TryGetValue(evt.JsonTime, out var existingEvent)
            || !ReferenceEquals(existingEvent, evt))
        {
            return;
        }

        eventsByTime.Remove(evt.JsonTime);
        var search = BinarySearchTime(evt.JsonTime);
        if (search >= 0)
        {
            eventTimes.RemoveAt(search);
        }
    }

    // Resolve the last authored boost at or before the requested event time.
    public bool IsBoostAt(float jsonTime)
    {
        var search = BinarySearchTime(jsonTime);
        var boostIndex = search >= 0
            ? search
            : (~search) - 1;
        return boostIndex >= 0 && eventsByTime[eventTimes[boostIndex]].Value == 1;
    }

    // Resolve the strict successor that bounds a boost's affected palette interval.
    private float GetNextTime(float jsonTime)
    {
        var search = BinarySearchTime(jsonTime);
        var nextIndex = search >= 0
            ? search + 1
            : ~search;
        return nextIndex < eventTimes.Count
            ? eventTimes[nextIndex]
            : float.PositiveInfinity;
    }

    // Mark the palette interval before or after a mutation while the index still represents that mutation state.
    public void InvalidateAppearanceRange(float boostTime)
    {
        appearanceStartTime = Mathf.Min(appearanceStartTime, boostTime);
        appearanceEndTime = Mathf.Max(appearanceEndTime, GetNextTime(boostTime));
    }

    // Refresh only loaded light events between the earliest changed boost and its next palette boundary.
    public void RefreshDependentAppearances(
        EventGridContainer gridContainer,
        Action<float, float> refreshGlsAppearances)
    {
        if (appearanceStartTime == float.PositiveInfinity)
        {
            return;
        }

        var events = gridContainer.MapObjects.AsSpan();
        var startIndex = events.BinarySearchBy(appearanceStartTime, evt => evt.JsonTime);
        if (startIndex < 0)
        {
            startIndex = ~startIndex;
        }
        else
        {
            // Include every stacked event at the first affected time, regardless of BinarySearch's equal-item result.
            while (startIndex > 0 && events[startIndex - 1].JsonTime >= appearanceStartTime)
            {
                startIndex--;
            }
        }

        var endIndex = events.Length;
        if (!float.IsPositiveInfinity(appearanceEndTime))
        {
            endIndex = events.BinarySearchBy(appearanceEndTime, evt => evt.JsonTime);
            if (endIndex < 0)
            {
                endIndex = ~endIndex;
            }
            else
            {
                // The next boost owns its own interval, so exclude every event stacked at its time.
                while (endIndex > 0 && events[endIndex - 1].JsonTime >= appearanceEndTime)
                {
                    endIndex--;
                }
            }
        }

        for (var i = startIndex; i < endIndex; i++)
        {
            var evt = events[i];
            // The index receives its runtime dependencies from the owning grid rather than relying on component state.
            if (gridContainer.BeatmapContext.TrackDefinitions.GetBasicOrDefault(evt.Type).Kind != BasicEventKind.Lights
                || !gridContainer.LoadedContainers.TryGetValue(evt, out var container))
            {
                continue;
            }

            (container as EventContainer).RefreshAppearance();
        }

        // GLS collections own separate preview containers, so propagate this exact palette interval before clearing it.
        refreshGlsAppearances?.Invoke(appearanceStartTime, appearanceEndTime);
        appearanceStartTime = float.PositiveInfinity;
        appearanceEndTime = float.NegativeInfinity;
    }

    // Return a matching timestamp index or the complement of its insertion point without allocating a range view.
    private int BinarySearchTime(float jsonTime)
    {
        var low = 0;
        var high = eventTimes.Count - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = eventTimes[middle].CompareTo(jsonTime);
            if (comparison < 0)
            {
                low = middle + 1;
            }
            else if (comparison > 0)
            {
                high = middle - 1;
            }
            else
            {
                return middle;
            }
        }

        return ~low;
    }
}
