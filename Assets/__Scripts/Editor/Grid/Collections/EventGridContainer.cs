using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

// Persist Basic Event light-ID page state through the same EditorData lifecycle used by GLS pages.
public class EventGridContainer : BeatmapObjectContainerCollection<BaseEvent>,
                                  CMInput.IEventGridActions,
                                  IEditorStateProvider
{
    public enum PropMode
    {
        Off, Prop, Light
    }

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private GridLane gridLane;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private CountersPlusController countersPlus;

    public int EventTypeToPropagate = (int)EventTypeValue.Event1;
    public int EventTypePropagationSize;

    // Isolate boost ordering and predecessor queries from the grid's rendering and invalidation responsibilities.
    private readonly ColorBoostEventIndex boostEventIndex = new();

    // Index only rendered Basic Event spans so chunk refreshes never scan the complete light-event map.
    private readonly TransitionIntervalIndex<BaseEvent> transitionRibbonIntervals = new();

    // Reuse the overlap result as both a retention set and creation list without allocating on viewport refreshes.
    private readonly HashSet<BaseEvent> visibleTransitionRibbonSources = new();

    // Reuse ID deduplication while one All Lights edit invalidates the latest scoped source in each affected lane.
    private readonly HashSet<int> interruptedRibbonLightIds = new();

    // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt needs logarithmic lookup of the next unscoped interrupt.
    private readonly Dictionary<int, List<BaseEvent>> allLightsInterruptsByType = new();

    // Let GLS preview collections repaint only the palette interval changed by a boost node edit.
    public event Action<float, float> OnBoostAppearanceRangeInvalidated;

    public List<BaseEvent> AllBpmEvents = new();

    private readonly HashSet<BaseEvent> lightEventsWithKnownPrevNext = new();

    // Keep propagation-off label refreshes independent of the complete basic-event map size.
    private readonly BasicEventNameFilterIndex nameFilterIndex = new();

    private Dictionary<int, List<BaseEvent>> allLightEvents = new();

    public Dictionary<int, List<BaseEvent>> AllLightEvents
    {
        get => allLightEvents;
        set
        {
            allLightEvents = value;
            // Rebuild the compact interrupt index once when a map load replaces every per-type event list.
            RebuildAllLightsInterruptIndex();
            foreach (var p in allLightEvents)
            {
                var lightList = p.Value;

                if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
                    LinkEventsForChroma(lightList);
                else
                    LinkEventsForVanilla(lightList);
            }

            // Rebuild once after bulk relinking so every indexed interval reflects the new authoritative successor.
            InitializeTransitionRibbonIntervals();
        }
    }

    public Dictionary<int, BasicLightEffect> TypeToManager = new();
    private PropMode propagationEditing = PropMode.Off;

    public override ObjectType ContainerType => ObjectType.Event;

    // Isolate the Basic Event light-ID page from placement and GLS component state.
    public string StateKey => "basicEventLightIdPage";

    public PropMode PropagationEditing
    {
        get => propagationEditing;
        set
        {
            propagationEditing = value;
            boxSelectionPlacement.Cancel();

            var propagationLength = 0;
            if (TypeToManager.TryGetValue(EventTypeToPropagate, out var lightingManager))
            {
                propagationLength =
                    (value == PropMode.Light
                        ? lightingManager.LaneToLightID?.Count
                        : lightingManager.LaneToLightIDs?.Count)
                    ?? 0;
            }

            labels.UpdateLabels(
                value,
                EventTypeToPropagate,
                propagationLength + 1);
            gridLane.Lane =
                value != PropMode.Off
                    ? propagationLength + 1
                    : labels.LaneCount;
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }

    public void OnToggleLightPropagation(InputAction.CallbackContext context)
    {
        if (context.performed) PropagationEditing = PropagationEditing == PropMode.Prop ? PropMode.Off : PropMode.Prop;
    }

    public void OnToggleLightIdMode(InputAction.CallbackContext context)
    {
        if (context.performed)
            PropagationEditing = PropagationEditing == PropMode.Light ? PropMode.Off : PropMode.Light;
    }

    public void OnResetRings(InputAction.CallbackContext context)
    {
        if (!context.performed || laserSpeedController.Activated) return;

        // if (descriptor.BigRingRotationManager is TrackLaneRingsRotationManager manager) manager.RotationEffect.Reset();
        //
        // if (descriptor.SmallRingRotationManager != null && descriptor.SmallRingRotationManager.RotationEffect != null)
        //     descriptor.SmallRingRotationManager.RotationEffect.Reset();
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        // Shared PageUp must only change light-ID lanes while Basic Events owns the active editing context.
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(EditingMode.BasicEvent)
            || PropagationEditing != PropMode.Light)
        {
            return;
        }
        var ids = TypeToManager.Keys.ToList();
        var id = ids.IndexOf(EventTypeToPropagate);

        EventTypeToPropagate = id == -1 ? ids.First() : ids[(int)Mathf.Repeat(id + 1, ids.Count)];
        PropagationEditing = PropagationEditing;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        // Shared PageDown must only change light-ID lanes while Basic Events owns the active editing context.
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(EditingMode.BasicEvent)
            || PropagationEditing != PropMode.Light)
        {
            return;
        }
        var ids = TypeToManager.Keys.ToList();
        var id = ids.IndexOf(EventTypeToPropagate);

        EventTypeToPropagate = id == -1 ? ids.First() : ids[(int)Mathf.Repeat(id - 1, ids.Count)];
        PropagationEditing = PropagationEditing;
    }

    public static string GetKeyForProp(PropMode mode)
    {
        if (mode == PropMode.Light) return "_lightID";

        return mode == PropMode.Prop ? "_propID" : null;
    }

    // Preserve the last light-ID page even from another Basic Event page,
    // while recording whether it was active at save time.
    public void CaptureEditorState(SimpleJSON.JSONObject data)
    {
        var lightIdPageActive = PropagationEditing == PropMode.Light;
        data["eventType"] = EventTypeToPropagate;
        data["active"] = lightIdPageActive;
    }

    // Restore after environment setup so the saved event type resolves against authoritative light managers.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (!data.HasKey("eventType"))
        {
            return;
        }

        var eventType = data["eventType"].AsInt;
        if (!TypeToManager.ContainsKey(eventType))
        {
            return;
        }

        EventTypeToPropagate = eventType;
        // Metadata written before inactive-page tracking only contained an event type because light-ID mode was active.
        var lightIdPageActive = !data.HasKey("active") || data["active"].AsBool;
        if (lightIdPageActive)
        {
            PropagationEditing = PropMode.Light;
        }
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        // Bind the map-scoped index before the environment reset asks labels to render propagation-off lanes.
        nameFilterIndex.EnsureFor(MapObjects);
        labels.NameFilterIndex = nameFilterIndex;
        TypeToManager = descriptor
            .BasicEventEffectManager.GetEffects<BasicLightEffect>()
            .ToDictionary(x => x.type, x => x.effect);
        PropagationEditing = PropMode.Off;
        // Register after environment setup so stale metadata cannot restore before light managers are authoritative.
        EditorStateService.Register(this);
    }

    internal override void SubscribeToCallbacks()
    {
        // Give labels their map-scoped index before any environment callback can request a refresh.
        labels.NameFilterIndex = nameFilterIndex;
        BeatmapContext.OnEnvironmentLoaded += HandleEnvironmentLoaded;
        SpawnCallbackController.OnEventPassedThreshold += SpawnCallback;
        SpawnCallbackController.OnRecursiveEventCheckFinished += OnRecursiveCheckFinished;
        DespawnCallbackController.OnEventPassedThreshold += DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        // Remove the destroyed grid from save-time EditorData capture.
        EditorStateService.Unregister(this);
        BeatmapContext.OnEnvironmentLoaded -= HandleEnvironmentLoaded;
        SpawnCallbackController.OnEventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.OnRecursiveEventCheckFinished -= OnRecursiveCheckFinished;
        DespawnCallbackController.OnEventPassedThreshold -= DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled -= OnPlayToggle;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            // Update the shared filter counts as the collection removes an authored basic event.
            if (!nameFilterIndex.EnsureFor(MapObjects))
            {
                nameFilterIndex.Remove(e);
            }

            if (e.IsColorBoostEvent())
            {
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
                boostEventIndex.Remove(e);
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
            }
            else if (e.IsBpmEvent())
            {
                AllBpmEvents.Remove(e);
            }
            else if (BeatmapContext.TrackDefinitions.GetBasicOrDefault(e.Type).Kind == BasicEventKind.Lights
                && !inCollection)
            {
                RemoveLinkedLightEvents(e);
                if (AllLightEvents.TryGetValue(e.Type, out var events))
                {
                    events.Remove(e);
                    // Keep the compact All Lights index synchronized with authoritative event removal.
                    RemoveFromAllLightsInterruptIndex(e);
                    // LightIdTransitionRibbonStopsAtAllLightsNonTransitionInterrupt requires restoring affected scoped lanes.
                    RefreshScopedRibbonSourcesInterruptedByAllLights(e, events);
                }
            }

            MarkEventToBeRelinked(e);
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        LinkAllLightEvents();
        LinkRingEvents();
        RefreshPool();
        RefreshVirtualLanes();
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            // Update the shared filter counts as the collection adds an authored basic event.
            if (!nameFilterIndex.EnsureFor(MapObjects))
            {
                nameFilterIndex.Add(e);
            }

            if (e.IsColorBoostEvent())
            {
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
                boostEventIndex.Add(e);
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
            }
            else if (e.IsBpmEvent())
            {
                AllBpmEvents.Add(e);
            }
            else if (BeatmapContext.TrackDefinitions.GetBasicOrDefault(e.Type).Kind == BasicEventKind.Lights
                && !inCollection)
            {
                RemoveLinkedLightEvents(e);
                // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt requires querying the new chronological interrupt.
                AddToAllLightEvents(e);
                LinkLightEvents(e);
                RefreshScopedRibbonSourcesInterruptedByAllLights(e, AllLightEvents[e.Type]);
                lightEventsWithKnownPrevNext.Add(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsSpawnedWorkflow()
    {
        LinkAllLightEvents();
        LinkRingEvents();
        RefreshVirtualLanes();
    }

    private void LinkLightEvents(BaseEvent e)
    {
        var previousEvent = GetPreviousEventWithSameLightIDOrDefault(e);
        if (previousEvent != null)
        {
            previousEvent.Next = e;
            if (LoadedContainers.TryGetValue(previousEvent, out var value))
                (value as EventContainer).RefreshAppearance();
        }

        var nextEvent = GetNextEventWithSameLightIDOrDefault(e);
        if (nextEvent != null) nextEvent.Prev = e;

        e.Prev = previousEvent;
        e.Next = nextEvent;
        // Only the inserted source and its predecessor can gain or lose a transition successor.
        UpdateTransitionRibbonInterval(previousEvent);
        UpdateTransitionRibbonInterval(e);
    }

    private void RemoveLinkedLightEvents(BaseEvent e)
    {
        // Remove the departing source before its predecessor is rewired to the following event.
        transitionRibbonIntervals.Remove(e);
        // Update appearance of previous event
        if (e.Prev != null)
        {
            if (e.Next != null)
                (e.Prev.Next, e.Next.Prev) = (e.Next, e.Prev);
            else
                e.Prev.Next = null;

            if (LoadedContainers.TryGetValue(e.Prev, out var prevContainer))
                (prevContainer as EventContainer).RefreshAppearance();

            // The predecessor now owns either a different transition interval or no ribbon at all.
            UpdateTransitionRibbonInterval(e.Prev);
        }
    }

    private void AddToAllLightEvents(BaseEvent e)
    {
        if (AllLightEvents.TryGetValue(e.Type, out var events))
        {
            // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt cannot use an ID-specific Prev as global order.
            events.Insert(FindFirstEventAfter(events, e.JsonTime), e);
        }
        else
            AllLightEvents.Add(e.Type, new List<BaseEvent> { e });

        // Keep effective scoped-lane successor queries logarithmic after incremental placement.
        AddToAllLightsInterruptIndex(e);
    }

    private void RebuildAllLightsInterruptIndex()
    {
        // Map-load rebuilds materialize only All Lights nodes while preserving each authoritative list's chronology.
        allLightsInterruptsByType.Clear();
        foreach (var pair in allLightEvents)
        {
            var events = pair.Value;
            for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                var evt = events[eventIndex];
                if (evt.CustomLightID == null || evt.CustomLightID.Length == 0)
                    AddToAllLightsInterruptIndex(evt);
            }
        }
    }

    private void AddToAllLightsInterruptIndex(BaseEvent evt)
    {
        // Scoped events cannot interrupt every lane and therefore do not belong in the compact index.
        if (evt.CustomLightID != null && evt.CustomLightID.Length > 0)
            return;

        if (!allLightsInterruptsByType.TryGetValue(evt.Type, out var interrupts))
        {
            interrupts = new List<BaseEvent>();
            allLightsInterruptsByType.Add(evt.Type, interrupts);
        }

        interrupts.Insert(FindFirstEventAfter(interrupts, evt.JsonTime), evt);
    }

    private void RemoveFromAllLightsInterruptIndex(BaseEvent evt)
    {
        // Removal is edit-boundary work; the small per-type interrupt list avoids touching every scoped event.
        if ((evt.CustomLightID != null && evt.CustomLightID.Length > 0)
            || !allLightsInterruptsByType.TryGetValue(evt.Type, out var interrupts))
        {
            return;
        }

        interrupts.Remove(evt);
        if (interrupts.Count == 0)
            allLightsInterruptsByType.Remove(evt.Type);
    }

    private void RefreshScopedRibbonSourcesInterruptedByAllLights(BaseEvent interrupt, List<BaseEvent> events)
    {
        // OEM transitions are already refreshed through LinkLightEvents' single chronological Prev/Next lane. The
        // LightIdTransitionRibbon interruption regressions need this additional scan only when Chroma splits that lane
        // by light ID, because one All Lights event can then invalidate several otherwise-unlinked scoped predecessors.
        if (!Settings.Instance.EmulateChromaAdvanced
            || !Settings.Instance.LightIDTransitionSupport
            || (interrupt.CustomLightID != null && interrupt.CustomLightID.Length > 0))
        {
            return;
        }

        interruptedRibbonLightIds.Clear();
        var eventIndex = FindFirstEventAfter(events, interrupt.JsonTime) - 1;
        for (; eventIndex >= 0; eventIndex--)
        {
            var source = events[eventIndex];
            if (source.JsonTime >= interrupt.JsonTime)
                continue;

            var sourceLightIds = source.CustomLightID;
            if (sourceLightIds == null || sourceLightIds.Length == 0)
                break;

            // Only the latest source for an ID can gain or lose this All Lights endpoint.
            if (!interruptedRibbonLightIds.Add(sourceLightIds[0]))
                continue;

            UpdateTransitionRibbonInterval(source);
            if (LoadedContainers.TryGetValue(source, out var sourceContainer))
                (sourceContainer as EventContainer).RefreshAppearance();
        }
    }

    // Bulk relinks already visit every light event, so rebuild the data-only interval tree once at that edit boundary.
    private void InitializeTransitionRibbonIntervals()
    {
        transitionRibbonIntervals.Clear();
        foreach (var lightEvents in allLightEvents.Values)
        {
            for (var eventIndex = 0; eventIndex < lightEvents.Count; eventIndex++)
            {
                UpdateTransitionRibbonInterval(lightEvents[eventIndex]);
            }
        }
    }

    // Replace only one source interval because insertion and deletion can change at most two successor links.
    private void UpdateTransitionRibbonInterval(BaseEvent source)
    {
        if (source == null)
        {
            return;
        }

        if (TryGetTransitionRibbonEndSongBpmTime(source, out var endSongBpmTime))
        {
            // AddOrReplace evicts the old key in the same lookup path before installing its updated interval.
            transitionRibbonIntervals.AddOrReplace(source, source.SongBpmTime, endSongBpmTime);
            return;
        }

        transitionRibbonIntervals.Remove(source);
    }

    // Mirror the two Basic Event appearance paths so the index retains exactly the span rendered by the source node.
    private bool TryGetTransitionRibbonEndSongBpmTime(BaseEvent source, out float endSongBpmTime)
    {
        endSongBpmTime = 0f;
        if (source.CustomLightGradient != null)
        {
            // Retain the exact SongBpmTime length rendered by LightGradientController for authored gradients.
            endSongBpmTime = source.SongBpmTime + source.CustomLightGradient.Duration;
            return endSongBpmTime >= source.SongBpmTime;
        }

        // LightIdTransitionRibbonStopsAtAllLightsNonTransitionInterrupt requires the index to stop at All Lights.
        var nextEvent = GetEffectiveNextLightEvent(source);
        if (source.IsFade
            || source.IsFlash
            || nextEvent == null
            || !nextEvent.IsTransition)
        {
            return false;
        }

        // Synthesized Basic Event ribbons end at the linked transition in the same event/light-ID lane.
        endSongBpmTime = nextEvent.SongBpmTime;
        return endSongBpmTime >= source.SongBpmTime;
    }

    private BaseEvent GetPreviousEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events)) return null;

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.FindLast(x => x.JsonTime < e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.FindLast(x => x.JsonTime < e.JsonTime);
    }

    private BaseEvent GetNextEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events)) return null;

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.Find(x => x.JsonTime > e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.Find(x => x.JsonTime > e.JsonTime);
    }

    public BaseEvent GetEffectiveNextLightEvent(BaseEvent source)
    {
        // Both LightIdTransitionRibbon interruption regressions share this effective endpoint without changing preview links.
        if (source == null
            || !Settings.Instance.EmulateChromaAdvanced
            || !Settings.Instance.LightIDTransitionSupport
            || source.CustomLightID == null
            || source.CustomLightID.Length == 0)
        {
            return source?.Next;
        }

        // Existing Prev/Next linking already provides the next same-ID node in O(1).
        var sameIdNext = source.Next;
        if (!allLightsInterruptsByType.TryGetValue(source.Type, out var interrupts))
            return sameIdNext;

        var interruptIndex = FindFirstEventAfter(interrupts, source.JsonTime);
        if (interruptIndex >= interrupts.Count)
            return sameIdNext;

        var allLightsNext = interrupts[interruptIndex];
        return sameIdNext == null || allLightsNext.JsonTime < sameIdNext.JsonTime
            ? allLightsNext
            : sameIdNext;
    }

    private static int FindFirstEventAfter(List<BaseEvent> events, float jsonTime)
    {
        // LightIdTransitionRibbonStopsAtAllLightsNonTransitionInterrupt requires the shared upper-bound helper so
        // stacked events remain ordered without maintaining a second manual binary-search implementation here.
        return events.AsSpan().UpperBoundBy(jsonTime, evt => evt.JsonTime);
    }

    // TODO: bleh, who cares about prop ID anyway
    // public override IEnumerable<BaseObject> GrabSortedObjects()
    // {
    //     var sorted = new List<BaseObject>();
    //     var grouping = LoadedObjects.GroupBy(x => x.Time);
    //     foreach (var group in grouping)
    //     {
    //         sorted.AddRange(@group.Where(x => x is BaseEvent).Cast<BaseEvent>().OrderBy(x =>
    //             x.CustomData.HasKey(x.CustomKeyPropID) ? x.CustomData[x.CustomKeyPropID].AsInt : -1)); // Sort non-light prop events before light prop events
    //     }
    //
    //     return sorted;
    // }

    private void RefreshVirtualLanes()
    {
        // Rebuild once after a map load that replaces the backing event list outside collection callbacks.
        nameFilterIndex.EnsureFor(MapObjects);
        labels.NameFilterIndex = nameFilterIndex;
        if (propagationEditing == PropMode.Off)
            PropagationEditing = PropMode.Off;
    }

    private void UpdatePropagationMode()
    {
        foreach (var con in LoadedContainers.Values)
        {
            if (con is not EventContainer e) continue;

            if (propagationEditing != PropMode.Off)
                con.SafeSetActive(e.EventData.Type == EventTypeToPropagate);
            else
                con.SafeSetActive(true);

            con.UpdateGridPosition();
        }

        if (propagationEditing == PropMode.Off) OnPlayToggle(BeatmapContext.Atsc.IsPlaying);
    }

    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            var e = objectData as BaseEvent;
            // Keep a ribbon's source container loaded until its visible destination reaches the despawn boundary.
            if (TryGetVisibleRibbonEndTime(e, out _))
                StartCoroutine(nameof(WaitForRibbonThenRecycle), e);
            else
                RecycleContainer(objectData);
        }
    }

    private IEnumerator WaitForRibbonThenRecycle(BaseEvent @event)
    {
        // Re-evaluate once when scheduled so custom gradients and linked Basic Event transitions share one lifetime rule.
        TryGetVisibleRibbonEndTime(@event, out var endTime);
        yield return new WaitUntil(() =>
            endTime < BeatmapContext.Atsc.CurrentJsonTime + DespawnCallbackController.Offset);
        RecycleContainer(@event);
    }

    private bool TryGetVisibleRibbonEndTime(BaseEvent @event, out float endTime)
    {
        endTime = 0f;
        if (!Settings.Instance.VisualizeChromaGradients || !isActiveAndEnabled)
            return false;

        // Authored Chroma gradients retain their existing duration-based lifetime.
        if (@event.CustomLightGradient != null)
        {
            endTime = @event.JsonTime + @event.CustomLightGradient.Duration;
            return true;
        }

        // Ribbon retention must use the same All Lights-aware endpoint as appearance and interaction.
        var nextEvent = GetEffectiveNextLightEvent(@event);
        if (BeatmapContext.TrackDefinitions.GetBasicOrDefault(@event.Type).Kind != BasicEventKind.Lights
            || @event.IsFade
            || @event.IsFlash
            || nextEvent == null
            || !nextEvent.IsTransition)
        {
            return false;
        }

        // Synthesized transition ribbons end at the linked destination event for the same light-ID lane.
        endTime = nextEvent.JsonTime;
        return true;
    }

    private void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
            // Cancel every delayed ribbon recycle before rebuilding the stopped timeline pool.
            StopCoroutine(nameof(WaitForRibbonThenRecycle));
            RefreshPool();
        }
    }

    private void OnRecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        var epsilon = Mathf.Pow(10, -9);
        RefreshPool(
            BeatmapContext.Atsc.CurrentSongBpmTime + DespawnCallbackController.Offset - epsilon,
            BeatmapContext.Atsc.CurrentSongBpmTime + SpawnCallbackController.Offset + epsilon);
    }

    public override ObjectContainer CreateContainer() =>
        EventContainer.SpawnEvent(
            this,
            null,
            BeatmapContext.TrackDefinitions,
            ref eventPrefab,
            ref labels);

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Query once so base recycling and missing-source creation share one allocation-free overlap result.
        visibleTransitionRibbonSources.Clear();
        if (Settings.Instance.VisualizeChromaGradients && isActiveAndEnabled)
        {
            transitionRibbonIntervals.GetSourcesAt(lowerBound, visibleTransitionRibbonSources);
        }

        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Recreate only sources whose point node is offscreen but whose ribbon crosses the lower pool boundary.
        foreach (var source in visibleTransitionRibbonSources)
        {
            if (source.HasMatchingTrack(TrackFilterID))
            {
                CreateContainerFromPool(source);
            }
        }

        boostEventIndex.RefreshDependentAppearances(this, RaiseBoostAppearanceRangeInvalidated);
    }

    protected override bool ShouldRetainContainerOutsideBounds(
        BaseObject obj,
        float lowerBound,
        float upperBound)
    {
        // The overlap query already proved the source interval crosses this refresh's lower boundary.
        return obj is BaseEvent evt && visibleTransitionRibbonSources.Contains(evt);
    }

    private void RaiseBoostAppearanceRangeInvalidated(float startJsonTime, float endJsonTime) =>
        OnBoostAppearanceRangeInvalidated?.Invoke(startJsonTime, endJsonTime);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var eventContainer = con as EventContainer;
        // Rebind pooled and cloned event containers to the active environment metadata whenever they receive event data.
        eventContainer.TrackDefinitions = BeatmapContext.TrackDefinitions;
        // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt resolves endpoints for pooled finalized visuals.
        eventAppearance.SetAppearance(
            eventContainer,
            true,
            IsBoostAt(obj.JsonTime),
            GetEffectiveNextLightEvent(eventContainer.EventData));
        var e = obj as BaseEvent;
        if (PropagationEditing != PropMode.Off && e.Type != EventTypeToPropagate) con.SafeSetActive(false);
    }

    private void LinkEventsForChroma(List<BaseEvent> events)
    {
        var mostRecentEventByLightId = new Dictionary<int, BaseEvent>();

        for (var i = 0; i < events.Count; ++i)
        {
            var evt = events[i];
            var thisLightID = evt.CustomLightID?.FirstOrDefault();
            if (lightEventsWithKnownPrevNext.Add(evt))
            {
                evt.Prev = null;
                if (mostRecentEventByLightId.TryGetValue(thisLightID ?? int.MinValue, out var previousEvent))
                {
                    evt.Prev = previousEvent;
                    previousEvent.Next = evt;
                }

                evt.Next = null;
                for (var j = i + 1; j < events.Count; j++)
                {
                    if (thisLightID == events[j].CustomLightID?.FirstOrDefault())
                    {
                        events[j].Prev = evt;
                        evt.Next = events[j];
                        break;
                    }
                }
            }

            // Default is int.MinValue because there's going some mapper that will use negative lightID
            mostRecentEventByLightId[thisLightID ?? int.MinValue] = evt;
        }
    }

    private void LinkEventsForVanilla(List<BaseEvent> events)
    {
        if (events.Count == 0) return;

        if (events.Count == 1)
        {
            events[0].Prev = null;
            events[0].Next = null;
            return;
        }

        events[0].Prev = null;
        events[0].Next = events[1];

        for (var i = 1; i < events.Count - 1; i++)
        {
            events[i].Prev = events[i - 1];
            events[i].Next = events[i + 1];
        }

        events[^1].Prev = events[^2];
        events[^1].Next = null;
    }

    public void MarkEventsToBeRelinked(IEnumerable<BaseEvent> events)
    {
        foreach (var e in events) MarkEventToBeRelinked(e);
    }

    public void MarkEventToBeRelinked(BaseEvent e)
    {
        lightEventsWithKnownPrevNext.Remove(e.Prev);
        lightEventsWithKnownPrevNext.Remove(e);
        lightEventsWithKnownPrevNext.Remove(e.Next);
    }

    public void LinkAllLightEvents() =>
        AllLightEvents = MapObjects
            .Where(x => BeatmapContext.TrackDefinitions.GetBasicOrDefault(x.Type).Kind == BasicEventKind.Lights)
            .GroupBy(x => x.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

    private void LinkRingEvents()
    {
        BaseEvent prevRotation = null;
        BaseEvent prevZoom = null;

        foreach (var e in MapObjects)
        {
            var components = BeatmapContext.TrackDefinitions.GetBasicOrDefault(e.Type).Components;
            if (components.HasFlag(BasicEventComponent.RingRotation))
            {
                if (prevRotation != null)
                {
                    prevRotation.Next = e;
                    e.Prev = prevRotation;
                }
                else
                {
                    e.Prev = null;
                }

                prevRotation = e;
            }
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            if (components.HasFlag(BasicEventComponent.RingZoom)
                || components.HasFlag(BasicEventComponent.SmoothStepRingZoom))
            {
                if (prevZoom != null)
                {
                    prevZoom.Next = e;
                    e.Prev = prevZoom;
                }
                else
                {
                    e.Prev = null;
                }

                prevZoom = e;
            }
        }

        if (prevRotation != null) prevRotation.Next = null;
        if (prevZoom != null) prevZoom.Next = null;
    }

    public bool IsBoostAt(float jsonTime)
    {
        return boostEventIndex.IsBoostAt(jsonTime);
    }

    // Keep map-load ownership on the grid while the index owns its data representation.
    public void LoadBoostEvents(IEnumerable<BaseEvent> events)
    {
        boostEventIndex.Load(events);
    }

    public override void SilentRemoveObject(BaseObject obj)
    {
        if (obj is not BaseEvent evt || !evt.IsColorBoostEvent())
        {
            base.SilentRemoveObject(obj);
            return;
        }

        if (!TryBinarySearch(evt, out _))
        {
            return;
        }

        // Alt-drag temporarily removes the authored boost, so invalidate both its old and replacement ranges.
        boostEventIndex.InvalidateAppearanceRange(evt.JsonTime);
        base.SilentRemoveObject(evt);
        boostEventIndex.Remove(evt);
        boostEventIndex.InvalidateAppearanceRange(evt.JsonTime);
    }

    public void RefreshEventsAppearance(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events)
        {
            if (evt.Prev != null && LoadedContainers.TryGetValue(evt.Prev, out var evtPrevContainer))
                (evtPrevContainer as EventContainer).RefreshAppearance();
            if (LoadedContainers.TryGetValue(evt, out var evtContainer))
                (evtContainer as EventContainer).RefreshAppearance();
        }
    }

    public void UpdateColor(Color red, Color redBoost, Color blue, Color blueBoost, Color white, Color whiteBoost)
    {
        eventAppearance.RedColor = red;
        eventAppearance.RedBoostColor = redBoost;
        eventAppearance.BlueColor = blue;
        eventAppearance.BlueBoostColor = blueBoost;
        eventAppearance.WhiteColor = white;
        eventAppearance.WhiteBoostColor = whiteBoost;
    }
}
