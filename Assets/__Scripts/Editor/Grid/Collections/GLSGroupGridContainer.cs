using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>
    where TGroup : BaseEventBoxGroup
{
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // Reuse the retention set because pool refreshes happen frequently while scrolling.
    private readonly System.Collections.Generic.HashSet<TGroup> retainedGroups = new();

    // Outer GLS queue previews use the finalized event grid's boost timeline at their represented node time.
    public bool IsBoostAt(float jsonTime) => eventGridContainer.IsBoostAt(jsonTime);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        eventGridContainer.OnBoostAppearanceRangeInvalidated += RefreshBoostDependentAppearances;
        // Rebuild loaded groups immediately when the ghost-preview setting changes.
        Settings.NotifyBySettingName(nameof(Settings.GLSOuterTrackGhostNodeOpacity), _ => RefreshPool(true));
    }
    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        eventGridContainer.OnBoostAppearanceRangeInvalidated -= RefreshBoostDependentAppearances;
    }

    private void RefreshBoostDependentAppearances(float startJsonTime, float endJsonTime)
    {
        foreach (var pair in LoadedContainers)
        {
            // LoadedContainers is base-typed, so restore this collection's GLS group type before querying preview data.
            if (pair.Key is not TGroup group)
            {
                continue;
            }

            var orderedEvents = group.OrderedEvents;
            if (HasPreviewEventInRange(orderedEvents, startJsonTime, endJsonTime))
            {
                // Rebuild this outer group so every affected ghost resolves boost at its own absolute event time.
                (pair.Value as GLSGroupContainer).ConfigurePreviewNodes(eventGridContainer.IsBoostAt);
            }
        }
    }

    private static bool HasPreviewEventInRange(
        System.Collections.Generic.List<BaseGLSEvent> orderedEvents,
        float startJsonTime,
        float endJsonTime)
    {
        // Reuse the shared sorted-list lookup so preview invalidation retains one binary-search implementation.
        var eventIndex = orderedEvents.BinarySearchBy(startJsonTime, evt => evt.JsonTime);
        if (eventIndex < 0)
        {
            eventIndex = ~eventIndex;
        }

        // BinarySearchBy can return the final item past the end of the range, so confirm the lower range bound too.
        return eventIndex < orderedEvents.Count
            && orderedEvents[eventIndex].JsonTime >= startJsonTime
            && orderedEvents[eventIndex].JsonTime < endJsonTime;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Keep a parent group loaded while its final preview node still overlaps the unload boundary.
        retainedGroups.Clear();
        foreach (var loadedObject in ObjectsWithContainers)
        {
            if (loadedObject is TGroup group
                && group.SongBpmTime < lowerBound
                && group.HasMatchingTrack(TrackFilterID)
                && GetLastPreviewTime(group) >= lowerBound)
            {
                retainedGroups.Add(group);
            }
        }

        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Restore a parent recycled by the normal start-time pool check so its preview ghosts remain visible.
        foreach (var group in retainedGroups)
        {
            if (!LoadedContainers.ContainsKey(group))
                CreateContainerFromPool(group);
        }
    }

    private static float GetLastPreviewTime(TGroup group)
    {
        var orderedEvents = group.OrderedEvents;
        if (orderedEvents.Count == 0)
        {
            return group.SongBpmTime;
        }

        // OrderedEvents is maintained when GLS previews are rebuilt, avoiding a nested box/event scan per pool refresh.
        return orderedEvents[orderedEvents.Count - 1].SongBpmTime;
    }

    public override ObjectContainer CreateContainer() =>
        GLSGroupContainer.SpawnGLSGroup(
            null,
            BeatmapContext.TrackDefinitions,
            ref eventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var e = obj as BaseEventBoxGroup;
        con.transform.SetParent(
            glsGroupGridProvider.IdToTracks.TryGetValue(e.ID, out var track)
                ? track.Track.ObjectParentTransform
                : TargetTransform,
            false);

        var pos = con.transform.localPosition;
        pos.x = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(BeatmapContext.TrackDefinitions, e);
        pos.y = 0.5f;
        con.transform.localPosition = pos;

        // Rebuild previews with boost evaluated at each represented inner event's absolute time.
        (con as GLSGroupContainer).ConfigurePreviewNodes(eventGridContainer.IsBoostAt);
    }
}
