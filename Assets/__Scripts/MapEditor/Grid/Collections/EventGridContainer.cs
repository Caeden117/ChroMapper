﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventGridContainer : BeatmapObjectContainerCollection<BaseEvent>, CMInput.IEventGridActions
{
    public enum PropMode
    {
        Off, Prop, Light
    }

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private GameObject eventGridLabels;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private BoxSelectionPlacementController boxSelectionPlacementController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private CountersPlusController countersPlus;

    public int EventTypeToPropagate = (int)EventTypeValue.RingLights;
    public int EventTypePropagationSize;

    public List<BaseEvent> AllRotationEvents = new List<BaseEvent>();
    public List<BaseEvent> AllBoostEvents = new List<BaseEvent>();
    public List<BaseEvent> AllBpmEvents = new List<BaseEvent>();

    private HashSet<BaseEvent> lightEventsWithKnownPrevNext = new HashSet<BaseEvent>();

    private Dictionary<int, List<BaseEvent>> allLightEvents = new Dictionary<int, List<BaseEvent>>();
    public Dictionary<int, List<BaseEvent>> AllLightEvents
    {
        get => allLightEvents;
        set
        {
            allLightEvents = value;
            foreach (var p in allLightEvents)
            {
                var lightList = p.Value;

                if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
                {
                    LinkEventsForChroma(lightList);
                }
                else
                {
                    LinkEventsForVanilla(lightList);
                }
            }
        }
    }

    internal PlatformDescriptor platformDescriptor;
    private PropMode propagationEditing = PropMode.Off;

    public override ObjectType ContainerType => ObjectType.Event;
    private static int ExtraInterscopeLanes => EventContainer.ModifyTypeMode == 2 ? 2 : 0;
    private int SpecialEventTypeCount => 7 + labels.NoRotationLaneOffset + ExtraInterscopeLanes;

    public PropMode PropagationEditing
    {
        get => propagationEditing;
        set
        {
            propagationEditing = value;
            boxSelectionPlacementController.CancelPlacement();

            var lightingManager = platformDescriptor.LightingManagers[EventTypeToPropagate];

            var propagationLength = lightingManager == null
                ? 0
                : (value == PropMode.Light
                    ? platformDescriptor.LightingManagers[EventTypeToPropagate].LightIDPlacementMapReverse?.Count
                    : platformDescriptor.LightingManagers[EventTypeToPropagate].LightsGroupedByZ?.Length) ?? 0;

            labels.UpdateLabels(value, EventTypeToPropagate,
                value == PropMode.Off ? 16 + ExtraInterscopeLanes : propagationLength + 1);
            eventPlacement.SetGridSize(value != PropMode.Off
                ? propagationLength + 1
                : SpecialEventTypeCount + platformDescriptor.LightingManagers.Count(s => s != null));
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

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

        if (platformDescriptor.BigRingManager is TrackLaneRingsManager manager)
            manager.RotationEffect.Reset();

        if (platformDescriptor.SmallRingManager != null && platformDescriptor.SmallRingManager.RotationEffect != null)
            platformDescriptor.SmallRingManager.RotationEffect.Reset();
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var nextID = EventTypeToPropagate + 1;
        if (nextID == platformDescriptor.LightingManagers.Length) nextID = 0;
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID++;
            if (nextID == platformDescriptor.LightingManagers.Length) nextID = 0;
        }

        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var nextID = EventTypeToPropagate - 1;
        if (nextID == -1) nextID = platformDescriptor.LightingManagers.Length - 1;
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID--;
            if (nextID == -1) nextID = platformDescriptor.LightingManagers.Length - 1;
        }

        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public static string GetKeyForProp(PropMode mode)
    {
        if (mode == PropMode.Light) return "_lightID";

        return mode == PropMode.Prop ? "_propID" : null;
    }

    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor;
        StartCoroutine(AfterPlatformLoaded());
    }

    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        PropagationEditing = PropMode.Off;
    }

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished -= RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    protected override void OnObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            if (e.IsLaneRotationEvent())
            {
                AllRotationEvents.Remove(e);
                tracksManager.RefreshTracks();
            }
            else if (e.IsColorBoostEvent())
            {
                AllBoostEvents.Remove(e);
            }
            else if (e.IsBpmEvent())
            {
                AllBpmEvents.Remove(e);
            }
            else if (e.IsLightEvent() && !inCollection)
            {
                RemoveLinkedLightEvents(e);

                if (AllLightEvents.TryGetValue(e.Type, out var events))
                {
                    events.Remove(e);
                }
            }

            MarkEventToBeRelinked(e);
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        LinkAllLightEvents();
        RefreshPool();
    }

    protected override void OnObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            if (e.IsLaneRotationEvent()) AllRotationEvents.Add(e);
            else if (e.IsColorBoostEvent()) AllBoostEvents.Add(e);
            else if (e.IsBpmEvent()) AllBpmEvents.Add(e);
            else if (e.IsLightEvent() && !inCollection)
            {
                RemoveLinkedLightEvents(e);
                LinkLightEvents(e);
                AddToAllLightEvents(e);
                lightEventsWithKnownPrevNext.Add(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsSpawnedWorkflow() => LinkAllLightEvents();

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
        if (nextEvent != null)
        {
            nextEvent.Prev = e;
        }

        e.Prev = previousEvent;
        e.Next = nextEvent;
    }

    private void RemoveLinkedLightEvents(BaseEvent e)
    {
        // Update appearance of previous event
        if (e.Prev != null)
        {
            if (e.Next != null)
                (e.Prev.Next, e.Next.Prev) = (e.Next, e.Prev);
            else
                e.Prev.Next = null;

            if (LoadedContainers.TryGetValue(e.Prev, out var prevContainer))
                (prevContainer as EventContainer).RefreshAppearance();
        }
    }

    private void AddToAllLightEvents(BaseEvent e)
    {
        if (AllLightEvents.TryGetValue(e.Type, out var events))
        {
            if (e.Prev == null)
                events.Add(e);
            else
                events.Insert(events.IndexOf(e.Prev) + 1, e);
        }
        else
        {
            AllLightEvents.Add(e.Type, new List<BaseEvent> { e });
        }
    }

    private BaseEvent GetPreviousEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events))
        {
            return null;
        }

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.FindLast(x => x.JsonTime < e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.FindLast(x => x.JsonTime < e.JsonTime);
    }

    private BaseEvent GetNextEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events))
        {
            return null;
        }

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.Find(x => x.JsonTime > e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.Find(x => x.JsonTime > e.JsonTime);
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

    private void UpdatePropagationMode()
    {
        foreach (var con in LoadedContainers.Values)
        {
            if (con is not EventContainer e) continue;

            if (propagationEditing != PropMode.Off)
            {
                con.SafeSetActive(e.EventData.Type == EventTypeToPropagate);
            }
            else
            {
                con.SafeSetActive(true);
            }

            con.UpdateGridPosition();
        }

        if (propagationEditing == PropMode.Off) OnPlayToggle(AudioTimeSyncController.IsPlaying);
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
            if (e.CustomLightGradient != null && Settings.Instance.VisualizeChromaGradients && isActiveAndEnabled)
                StartCoroutine(nameof(WaitForGradientThenRecycle), e);
            else
                RecycleContainer(objectData);
        }
    }

    private IEnumerator WaitForGradientThenRecycle(BaseEvent @event)
    {
        var endTime = @event.JsonTime + @event.CustomLightGradient.Duration;
        yield return new WaitUntil(() =>
            endTime < AudioTimeSyncController.CurrentJsonTime + DespawnCallbackController.Offset);
        RecycleContainer(@event);
    }

    private void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
            StopCoroutine(nameof(WaitForGradientThenRecycle));
            RefreshPool();
        }
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        var epsilon = Mathf.Pow(10, -9);
        RefreshPool(AudioTimeSyncController.CurrentSongBpmTime + DespawnCallbackController.Offset - epsilon,
            AudioTimeSyncController.CurrentSongBpmTime + SpawnCallbackController.Offset + epsilon);
    }

    public override ObjectContainer CreateContainer() =>
        EventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSo, ref labels);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        eventAppearanceSo.SetEventAppearance(con as EventContainer, true,
            AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1);
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
        if (events.Count == 0)
        {
            return;
        }

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
        foreach (var e in events)
        {
            MarkEventToBeRelinked(e);
        }
    }

    public void MarkEventToBeRelinked(BaseEvent e)
    {
        lightEventsWithKnownPrevNext.Remove(e.Prev);
        lightEventsWithKnownPrevNext.Remove(e);
        lightEventsWithKnownPrevNext.Remove(e.Next);
    }

    public void LinkAllLightEvents() =>
        AllLightEvents = MapObjects.
            Where(x => x.IsLightEvent())
            .GroupBy(x => x.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

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
}
