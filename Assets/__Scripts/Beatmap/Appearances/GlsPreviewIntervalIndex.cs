using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

/// <summary>
///     Indexes GLS preview-node JSON-time spans for box-selection overlap queries.
/// </summary>
/// <remarks>
///     Parent groups can leave the viewport before their later preview nodes. Before this index, selection scanned
///     loaded containers and selected objects on every drag update: <c>O(log(G) + R + V + S)</c>. This index queries
///     authoritative preview spans in <c>O(log(I) + K)</c>; it rebuilds in <c>O(I * log(I))</c> only after GLS data changes.
///     <c>G</c> is all groups, <c>R</c> is parent starts in range, <c>V</c> is loaded containers, <c>S</c> is selected
///     objects, <c>I</c> is indexed preview spans, and <c>K</c> is overlapping spans returned by the query.
/// </remarks>
internal sealed class GlsPreviewIntervalIndex
{
    private readonly List<PreviewInterval> intervals = new();
    private readonly List<float> prefixMaximumEnds = new();
    private readonly Dictionary<BaseEventBoxGroup, Action<BaseEventBoxGroup>> groupChangeSubscriptions = new();
    private readonly List<BaseEventBoxGroup> staleSubscriptionGroups = new();
    private bool isDirty = true;
    private bool hasCollectionChangeSubscription;
    private int objectCount = -1;

    /// <summary>
    ///     Adds groups whose preview-node spans overlap the supplied selection interval.
    /// </summary>
    /// <remarks>
    ///     The result collection is caller-owned and reused across drag frames. This method does not inspect
    ///     <c>LoadedContainers</c>; all candidates come from the authoritative backing collection and event data.
    /// </remarks>
    public void AddOverlappingPreviewIntervals<TGroup>(
        BeatmapObjectContainerCollection<TGroup> collection,
        float startJsonTime,
        float endJsonTime,
        HashSet<BaseEventBoxGroup> candidates)
        where TGroup : BaseEventBoxGroup
    {
        if (!hasCollectionChangeSubscription)
        {
            // Mark the data-only index stale when its authoritative group collection changes.
            collection.OnObjectSpawned += _ => isDirty = true;
            collection.OnObjectDeleted += _ => isDirty = true;
            hasCollectionChangeSubscription = true;
        }

        EnsureCurrent(collection);
        if (intervals.Count == 0)
        {
            return;
        }

        // BinarySearchBy finds the first interval beginning after the selection without maintaining a second search.
        var intervalIndex = intervals.BinarySearchBy(endJsonTime, interval => interval.StartJsonTime);
        if (intervalIndex < 0)
        {
            intervalIndex = ~intervalIndex;
        }

        while (intervalIndex > 0 && intervals[intervalIndex - 1].StartJsonTime == endJsonTime)
        {
            intervalIndex--;
        }

        while (intervalIndex < intervals.Count && intervals[intervalIndex].StartJsonTime <= endJsonTime)
        {
            intervalIndex++;
        }

        for (var index = intervalIndex - 1; index >= 0; index--)
        {
            if (prefixMaximumEnds[index] < startJsonTime)
            {
                break;
            }

            var interval = intervals[index];
            if (interval.EndJsonTime >= startJsonTime)
            {
                candidates.Add(interval.Group);
            }
        }
    }

    // Share lazy event-cache initialization through BaseEventBoxGroup instead of dispatching every concrete GLS type.
    public static bool TryGetOrderedEvents(
        BaseEventBoxGroup group,
        out IReadOnlyList<BaseGLSEvent> orderedEvents)
    {
        if (group is not (BaseLightColorEventBoxGroup or ILightTransformEventBoxGroup or BaseVfxEventEventBoxGroup))
        {
            orderedEvents = null;
            return false;
        }

        EnsureOrderedEvents(group);
        orderedEvents = group.OrderedEvents;
        return true;
    }

    private void EnsureCurrent<TGroup>(BeatmapObjectContainerCollection<TGroup> collection)
        where TGroup : BaseEventBoxGroup
    {
        if (!isDirty && objectCount == collection.MapObjects.Count)
        {
            return;
        }

        intervals.Clear();
        prefixMaximumEnds.Clear();
        var currentGroups = new HashSet<BaseEventBoxGroup>();
        foreach (var group in collection.MapObjects)
        {
            currentGroups.Add(group);
            SubscribeToGroup(group);
            if (!TryGetOrderedEvents(group, out var orderedEvents)
                || orderedEvents.Count == 0)
            {
                continue;
            }

            intervals.Add(new PreviewInterval(
                group,
                group.JsonTime + orderedEvents[0].RelativeJsonTime,
                group.JsonTime + orderedEvents[orderedEvents.Count - 1].RelativeJsonTime));
        }

        // Buffer removed groups before mutation so a stale map object cannot retain this selection index.
        staleSubscriptionGroups.Clear();
        foreach (var group in groupChangeSubscriptions.Keys)
        {
            if (!currentGroups.Contains(group))
            {
                staleSubscriptionGroups.Add(group);
            }
        }

        foreach (var group in staleSubscriptionGroups)
        {
            group.OnOrderedEventsResorted -= groupChangeSubscriptions[group];
            groupChangeSubscriptions.Remove(group);
        }

        intervals.Sort(static (left, right) => left.StartJsonTime.CompareTo(right.StartJsonTime));
        var maximumEnd = float.NegativeInfinity;
        foreach (var interval in intervals)
        {
            maximumEnd = Mathf.Max(maximumEnd, interval.EndJsonTime);
            prefixMaximumEnds.Add(maximumEnd);
        }

        // Record the authoritative collection size after refreshing logical preview intervals.
        objectCount = collection.MapObjects.Count;
        isDirty = false;
    }

    // Subscribe to each authoritative group so node edits invalidate only its owning collection's index.
    private void SubscribeToGroup(BaseEventBoxGroup group)
    {
        if (groupChangeSubscriptions.ContainsKey(group))
        {
            return;
        }

        Action<BaseEventBoxGroup> onResorted = _ => isDirty = true;
        group.OnOrderedEventsResorted += onResorted;
        groupChangeSubscriptions.Add(group, onResorted);
    }

    private static void EnsureOrderedEvents(BaseEventBoxGroup group)
    {
        if (!group.OrderedEventsInitialized)
        {
            group.ResortOrderedEvents();
        }
    }

    private readonly struct PreviewInterval
    {
        public PreviewInterval(BaseEventBoxGroup group, float startJsonTime, float endJsonTime)
        {
            Group = group;
            StartJsonTime = startJsonTime;
            EndJsonTime = endJsonTime;
        }

        public BaseEventBoxGroup Group { get; }

        public float StartJsonTime { get; }

        public float EndJsonTime { get; }
    }
}
