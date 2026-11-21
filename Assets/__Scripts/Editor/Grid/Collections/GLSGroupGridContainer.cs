using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGLSGroupContainerCollection
{
    void RebindLoadedGroup(BaseObject oldObject, BaseObject newObject);
}

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>, IGLSGroupContainerCollection
    where TGroup : BaseEventBoxGroup
{
    private const double DefaultTargetFrameRate = 60d;
    private const int UncappedTargetFrameRateThreshold = 1000;
    private const double RemainingFrameReserveSeconds = 0.001d;
    // Reserve a small ribbon-upload budget even when the base frame misses its deadline so queued ribbons
    // keep making progress.
    private const double MinimumRibbonWorkSeconds = 0.002d;
    private const int MaximumPreviewConfigurationStepsPerFrame = 24;
    private const float PlaybackPreviewWindowStepBeats = 0.125f;

    private readonly struct PendingPreviewConfiguration : System.IComparable<PendingPreviewConfiguration>
    {
        public PendingPreviewConfiguration(GLSGroupContainer container, long sequence)
        {
            Container = container;
            Priority = container.CalculatePreviewConfigurationPrioritySongBpmTime();
            Sequence = sequence;
        }

        public GLSGroupContainer Container { get; }
        private float Priority { get; }
        private long Sequence { get; }

        public int CompareTo(PendingPreviewConfiguration other)
        {
            var comparison = other.Priority.CompareTo(Priority);
            return comparison != 0
                ? comparison
                : other.Sequence.CompareTo(Sequence);
        }
    }

    private readonly struct ActivePageContainerPoolFilter : IContainerPoolFilter
    {
        private readonly HashSet<int> activeGroupIds;

        public ActivePageContainerPoolFilter(HashSet<int> activeGroupIds) => this.activeGroupIds = activeGroupIds;

        public bool Includes(TGroup group) => activeGroupIds.Contains(group.ID);
    }

    [SerializeField] protected GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // Reuse the retention set because pool refreshes happen frequently while scrolling.
    private readonly HashSet<TGroup> retainedGroups = new();

    protected readonly HashSet<BaseGLSEvent> RetainedPreviewEvents = new();
    private readonly Dictionary<BaseEventBoxGroup, HashSet<BaseGLSEvent>> retainedPreviewEventsByGroup = new();
    private readonly Stack<HashSet<BaseGLSEvent>> spareRetainedPreviewEventSets = new();
    // Reusable list keeps later beats first so the earliest ribbon pops from the end without shifting.
    private readonly List<PendingPreviewConfiguration> pendingPreviewConfigurations = new();
    private readonly HashSet<GLSGroupContainer> reboundContainers = new();
    // Reuse still-active bodies before inactive ones so a rebind does not unregister and rebuild their labels.
    private readonly Stack<ObjectContainer> activePooledContainers = new();
    // Reusable query set for boost-spanning color sources; cleared and refilled per invalidation.
    private readonly HashSet<BaseLightColorEventBoxGroup> boostRefreshSourceGroups = new();
    private long previewConfigurationSequence;
    private float previewLowerBound;
    private float previewUpperBound;
    private bool deferAutomaticPreviewConfiguration;
    private bool deferPooledContainerDeactivation;
    private int lastPlaybackPreviewWindowIndex = int.MinValue;

    // Outer GLS queue previews use the finalized event grid's boost timeline at their represented node time.
    public bool IsBoostAt(float jsonTime) => eventGridContainer.IsBoostAt(jsonTime);

    // Keep the hovered preview collider alive while replacing its parent group's data.
    public void RebindLoadedGroup(BaseObject oldObject, BaseObject newObject)
    {
        if (oldObject == null
            || newObject is not TGroup newGroup
            || oldObject.ObjectType != newObject.ObjectType
            || !LoadedContainers.TryGetValue(oldObject, out var objectContainer)
            || objectContainer is not GLSGroupContainer groupContainer)
        {
            return;
        }

        LoadedContainers.Remove(oldObject);
        LoadedContainers.Add(newObject, groupContainer);
        var containerIndex = ObjectsWithContainers.IndexOf(oldObject);
        if (containerIndex >= 0)
            ObjectsWithContainers[containerIndex] = newObject;

        oldObject.HasAttachedContainer = false;
        newObject.HasAttachedContainer = true;
        groupContainer.RebindEventBoxGroup(newGroup);
        UpdateContainerData(groupContainer, newObject);
    }

    protected override ObjectContainer GetPooledContainer() => activePooledContainers.Count > 0
        ? activePooledContainers.Pop()
        : base.GetPooledContainer();

    protected override void DeactivatePooledContainer(ObjectContainer container)
    {
        if (!deferPooledContainerDeactivation)
            base.DeactivatePooledContainer(container);
    }

    protected override void AddPooledContainer(ObjectContainer container)
    {
        if (deferPooledContainerDeactivation)
            activePooledContainers.Push(container);
        else
            base.AddPooledContainer(container);
    }

    private void DeactivateUnusedActivePooledContainers()
    {
        while (activePooledContainers.Count > 0)
        {
            var container = activePooledContainers.Pop();
            base.DeactivatePooledContainer(container);
            ((GLSGroupContainer)container).DeactivateUnusedPooledPreviewRoot();
            base.AddPooledContainer(container);
        }
    }

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        UIMode.OnPreviewModeSwitched += HandlePreviewModeChanged;
        BeatmapContext.OnBeatmapTimingChanged += HandleBeatmapTimingChanged;
        glsGroupGridProvider.OnGroupPageChanged += HandleGroupPageChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated += RefreshBoostDependentAppearances;
        Settings.NotifyBySettingName(nameof(Settings.GLSOuterTrackGhostNodeOpacity), HandlePreviewSettingChanged);
        Settings.NotifyBySettingName(nameof(Settings.GLSInnerEventPreviewShrink), HandlePreviewSettingChanged);
        Settings.NotifyBySettingName(nameof(Settings.EnableGLSGhostPreview), HandlePreviewSettingChanged);
    }
    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        UIMode.OnPreviewModeSwitched -= HandlePreviewModeChanged;
        BeatmapContext.OnBeatmapTimingChanged -= HandleBeatmapTimingChanged;
        glsGroupGridProvider.OnGroupPageChanged -= HandleGroupPageChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated -= RefreshBoostDependentAppearances;
        // Remove only this collection's callbacks. Clearing all notifications would detach unrelated
        // observers.
        Settings.StopNotifyingBySettingName(nameof(Settings.GLSOuterTrackGhostNodeOpacity), HandlePreviewSettingChanged);
        Settings.StopNotifyingBySettingName(nameof(Settings.GLSInnerEventPreviewShrink), HandlePreviewSettingChanged);
        Settings.StopNotifyingBySettingName(nameof(Settings.EnableGLSGhostPreview), HandlePreviewSettingChanged);
    }

    private void HandleBeatmapTimingChanged(float _) => RefreshPool(true);

    private void HandlePreviewSettingChanged(object _) => RefreshPool(true);

    private void RefreshBoostDependentAppearances(float startJsonTime, float endJsonTime)
    {
        // Groups whose source interval already crosses the boundary span the edited boost even when every
        // node inside it belongs to a later group; nodes inside the interval still match by time.
        boostRefreshSourceGroups.Clear();
        if (typeof(TGroup) == typeof(BaseLightColorEventBoxGroup))
        {
            var songContainer = BeatSaberSongContainer.Instance;
            var map = songContainer != null
                ? songContainer.Map
                : null;
            var startSongBpmTime = map?.JsonTimeToSongBpmTime(startJsonTime) ?? startJsonTime;
            GLSEventCommon.GetColorTransitionSourceGroupsAt(
                startSongBpmTime,
                TrackFilterID,
                glsGroupGridProvider.ActiveGlsTrackIds,
                boostRefreshSourceGroups);
        }

        foreach (var pair in LoadedContainers)
        {
            // LoadedContainers is base-typed, so restore this collection's GLS group type before querying preview data.
            if (pair.Key is not TGroup group)
            {
                continue;
            }

            var orderedEvents = group.OrderedEvents;
            if (HasPreviewEventInRange(orderedEvents, startJsonTime, endJsonTime)
                || (group is BaseLightColorEventBoxGroup colorGroup
                    && boostRefreshSourceGroups.Contains(colorGroup)))
            {
                (pair.Value as GLSGroupContainer).ConfigurePreviewNodes(
                    eventGridContainer.IsBoostAt,
                    previewLowerBound,
                    previewUpperBound,
                    GetRetainedPreviewEvents(group),
                    true,
                    eventGridContainer.BoostEvents);
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
        lastPlaybackPreviewWindowIndex = int.MinValue;
        if (!playing) RefreshPool();
    }

    private void HandleGroupPageChanged(string _) => RefreshPool();

    private void HandlePreviewModeChanged()
    {
        if (!UIMode.PreviewMode && enabled)
        {
            RefreshPool(true);
        }
    }

    internal override void LateUpdate()
    {
        // Preview and Playing hide the GLS grid. Rebuild its window when the editor becomes visible again.
        if (UIMode.PreviewMode)
            return;

        deferAutomaticPreviewConfiguration = true;
        // Eighth-beat steps spread incoming label allocations without repeating the full pool query every frame.
        if (BeatmapContext.Atsc.IsPlaying)
        {
            var windowIndex = (int)Math.Floor(BeatmapContext.Atsc.CurrentSongBpmTime / PlaybackPreviewWindowStepBeats);
            if (windowIndex != lastPlaybackPreviewWindowIndex)
            {
                RefreshPool();
                lastPlaybackPreviewWindowIndex = windowIndex;
            }
        }
        else
        {
            base.LateUpdate();
        }
        deferAutomaticPreviewConfiguration = false;
        ProcessNextPreviewConfiguration();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        deferPooledContainerDeactivation = true;
        if (!deferAutomaticPreviewConfiguration)
            CancelPendingPreviewConfigurations();

        previewLowerBound = lowerBound;
        previewUpperBound = upperBound;
        PrepareRetainedPreviewEvents(lowerBound);
        IndexRetainedPreviewEvents();

        // Keep a parent group loaded while its final preview node still overlaps the unload boundary.
        retainedGroups.Clear();
        foreach (var loadedObject in ObjectsWithContainers)
        {
            if (loadedObject is TGroup group
                && group.SongBpmTime < lowerBound
                && IsGroupOnActivePage(group.ID)
                && group.HasMatchingTrack(TrackFilterID)
                && GetLastPreviewTime(group) >= lowerBound)
            {
                retainedGroups.Add(group);
            }
        }

        base.RefreshPool(
            lowerBound,
            upperBound,
            forceRefresh,
            new ActivePageContainerPoolFilter(glsGroupGridProvider.ActiveGlsTrackIds));

        // Restore a parent recycled by the normal start-time pool check so its preview ghosts remain visible.
        foreach (var group in retainedGroups)
        {
            if (!LoadedContainers.ContainsKey(group))
                CreateContainerFromPool(group);
        }

        foreach (var container in LoadedContainers.Values)
        {
            var groupContainer = container as GLSGroupContainer;
            if (groupContainer != null)
            {
                ConfigureLoadedGroup(groupContainer);
            }
        }

        // The active pool already holds only the released owners this refresh did not reuse.
        foreach (var unusedContainer in activePooledContainers)
            SchedulePreviewSuspension((GLSGroupContainer)unusedContainer);
    }

    private void SchedulePreviewConfiguration(GLSGroupContainer container, bool hideUntilConfigured)
    {
        var retainedEvents = GetRetainedPreviewEvents(container.EventBoxGroupData);
        // Preserve pending ribbon jobs when the visible node identities and window bounds are unchanged.
        if (!hideUntilConfigured
            && container.HasSamePreviewNodeWindow(previewLowerBound, previewUpperBound, retainedEvents))
        {
            return;
        }

        container.BeginPreviewNodeConfiguration(
            eventGridContainer.IsBoostAt,
            previewLowerBound,
            previewUpperBound,
            retainedEvents,
            false,
            hideUntilConfigured,
            true,
            eventGridContainer.BoostEvents);
        // Finish visible node binding before returning from the scrub refresh so nodes never wait for the
        // ribbon budget.
        while (!container.ProcessPreviewNodeConfigurationStep())
        {
        }

        RemovePendingPreviewConfiguration(container);
        if (container.HasPendingPreviewRibbons)
            EnqueuePreviewConfiguration(container);
    }

    // Color sources retained after the pool sweep must also bind their nodes before this refresh returns.
    protected void ConfigureLoadedGroup(GLSGroupContainer groupContainer)
    {
        if (deferAutomaticPreviewConfiguration)
        {
            SchedulePreviewConfiguration(
                groupContainer,
                reboundContainers.Remove(groupContainer));
        }
        else
        {
            groupContainer.ConfigurePreviewNodes(
                eventGridContainer.IsBoostAt,
                previewLowerBound,
                previewUpperBound,
                GetRetainedPreviewEvents(groupContainer.EventBoxGroupData),
                boostEvents: eventGridContainer.BoostEvents);
        }
    }

    private void SchedulePreviewSuspension(GLSGroupContainer container)
    {
        container.SuspendPreviewGhosts();
        RemovePendingPreviewConfiguration(container);
    }

    private void ProcessNextPreviewConfiguration()
    {
        var frameDeadline = Math.Max(
            GetCurrentFrameDeadline(),
            Time.realtimeSinceStartupAsDouble + MinimumRibbonWorkSeconds);
        var processedSteps = 0;
        while (pendingPreviewConfigurations.Count > 0
            && processedSteps < MaximumPreviewConfigurationStepsPerFrame)
        {
            var queuedEntry = pendingPreviewConfigurations[^1];
            pendingPreviewConfigurations.RemoveAt(pendingPreviewConfigurations.Count - 1);
            var container = queuedEntry.Container;
            if (container == null)
                continue;

            if (!container.ProcessNextPreviewRibbon())
            {
                // Recalculate the queue priority after each ribbon because the next ghost may be at a
                // different beat.
                EnqueuePreviewConfiguration(container);
            }

            processedSteps++;
            if (Time.realtimeSinceStartupAsDouble >= frameDeadline)
                return;
        }
        DeactivateUnusedActivePooledContainers();
        deferPooledContainerDeactivation = false;
    }

    private static double GetCurrentFrameDeadline()
    {
        var displayRefreshRate = Screen.currentResolution.refreshRateRatio.value;
        double targetFrameRate;
        if (QualitySettings.vSyncCount > 0 && displayRefreshRate > 0d)
        {
            targetFrameRate = displayRefreshRate / QualitySettings.vSyncCount;
        }
        else if (Application.targetFrameRate > 0
            && Application.targetFrameRate <= UncappedTargetFrameRateThreshold)
        {
            targetFrameRate = Application.targetFrameRate;
        }
        else
        {
            // ChroMapper's default 9999 FPS means uncapped. Use the monitor as its practical presentation
            // target.
            targetFrameRate = displayRefreshRate > 0d
                ? displayRefreshRate
                : DefaultTargetFrameRate;
        }

        var targetFrameDuration = 1d / targetFrameRate;
        var reserve = Math.Min(RemainingFrameReserveSeconds, targetFrameDuration * 0.25d);
        // Unity samples unscaledTime at frame start. The scheduler compares only the running clock per
        // ribbon.
        return Time.unscaledTimeAsDouble + targetFrameDuration - reserve;
    }

    private void EnqueuePreviewConfiguration(GLSGroupContainer container)
    {
        var entry = new PendingPreviewConfiguration(container, previewConfigurationSequence++);
        var index = pendingPreviewConfigurations.BinarySearch(entry);
        pendingPreviewConfigurations.Insert(~index, entry);
    }

    private void RemovePendingPreviewConfiguration(GLSGroupContainer container)
    {
        for (var index = pendingPreviewConfigurations.Count - 1; index >= 0; index--)
        {
            if (!ReferenceEquals(pendingPreviewConfigurations[index].Container, container))
                continue;

            pendingPreviewConfigurations.RemoveAt(index);
            return;
        }
    }

    internal GLSGroupContainer NextPendingPreviewConfiguration => pendingPreviewConfigurations.Count > 0
        ? pendingPreviewConfigurations[^1].Container
        : null;

    private void CancelPendingPreviewConfigurations()
    {
        foreach (var entry in pendingPreviewConfigurations)
        {
            if (entry.Container != null)
                entry.Container.CancelPreviewNodeConfiguration();
        }

        pendingPreviewConfigurations.Clear();
        reboundContainers.Clear();
    }

    protected virtual void PrepareRetainedPreviewEvents(float lowerBound) => RetainedPreviewEvents.Clear();

    private void IndexRetainedPreviewEvents()
    {
        // Group the offscreen ribbon sources by container so each group visits only its own nodes.
        foreach (var events in retainedPreviewEventsByGroup.Values)
        {
            events.Clear();
            spareRetainedPreviewEventSets.Push(events);
        }

        retainedPreviewEventsByGroup.Clear();

        foreach (var previewEvent in RetainedPreviewEvents)
        {
            var group = previewEvent.EventBoxGroupData;
            if (!retainedPreviewEventsByGroup.TryGetValue(group, out var events))
            {
                events = spareRetainedPreviewEventSets.Count > 0
                    ? spareRetainedPreviewEventSets.Pop()
                    : new HashSet<BaseGLSEvent>();
                retainedPreviewEventsByGroup.Add(group, events);
            }

            events.Add(previewEvent);
        }
    }

    private ISet<BaseGLSEvent> GetRetainedPreviewEvents(BaseEventBoxGroup group) =>
        group != null && retainedPreviewEventsByGroup.TryGetValue(group, out var events)
            ? events
            : null;

    protected bool IsGroupOnActivePage(int groupId) => glsGroupGridProvider.ActiveGlsTrackIds.Contains(groupId);

    protected override bool ShouldRetainContainerOutsideBounds(
        BaseObject obj,
        float lowerBound,
        float upperBound) => obj is TGroup group
        && IsGroupOnActivePage(group.ID)
        && retainedGroups.Contains(group);

    protected override void HandleContainerDespawn(ObjectContainer container, BaseObject obj) =>
        ((GLSGroupContainer)container).ResetForPool(deferPooledContainerDeactivation);

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

        var groupContainer = con as GLSGroupContainer;
        groupContainer.BindRibbonLane(track != null ? track.GridLane : null);
        // Pool reuse can change the group ID, so rebind its environment light
        // count before constructing the new node's per-light previews.
        groupContainer.GlsLightCount = BeatmapContext.GetGlsLightCount(e.ID);
        if (deferAutomaticPreviewConfiguration)
        {
            if (groupContainer.HasActivePooledPreviewRoot)
                groupContainer.SetPreviewParent(con.transform.parent);
            else
                reboundContainers.Add(groupContainer);
        }
        else
        {
            groupContainer.SetPreviewParent(con.transform.parent);
            groupContainer.ConfigurePreviewNodes(
                eventGridContainer.IsBoostAt,
                previewLowerBound,
                previewUpperBound,
                GetRetainedPreviewEvents(e),
                boostEvents: eventGridContainer.BoostEvents);
        }
    }
}
