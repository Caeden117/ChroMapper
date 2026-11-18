using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// Base class for managing GLS event group states and effects.
/// IMPORTANT: This class manages the state caching system for GLS event groups.
/// States are stored in StateChunksContainer buckets based on their StartTime (SongBpmTime).
/// 
/// CRITICAL: When an event group's JsonTime changes (e.g., when moved via cut/paste),
/// the state must be properly removed and re-inserted via the StateManager's RemoveData/InsertData
/// mechanism. The UpdateData method in GLSManager handles this by:
/// 1. Removing the old state using the original time (to find it in the correct bucket)
/// 2. Inserting a new state with the updated time (to place it in the correct bucket)
/// 
/// If the state is not properly updated, the renderer will continue showing lights at the old
/// location instead of the new location after the event group is moved.
/// </summary>
public abstract class
    EventGroupEffect<TGroupState, TEventState, TGroup, TBox, TEvent> : StateManager<TGroupState, TGroup>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    [SerializeField] public int Count;

    private static bool IsAuthoredBox(TBox box) => !box.IsAutomaticAxisLane || box.ReadOnlyEvents.Count > 0;

    public void RecomputeSongBpmTimes(float jsonTime, bool hasGroups)
    {
        // Clear cached per-light states when this GLS event type is removed from the map.
        if (!hasGroups)
        {
            Initialize();
            Refresh();
            return;
        }

        var map = BeatSaberSongContainer.Instance.Map;
        var boundary = (float)map.JsonTimeToSongBpmTime(jsonTime);
        Func<TGroupState, float> groupTime = state => state.Base.SongBpmTime;
        Func<TEventState, float> eventTime = state => (float)map.JsonTimeToSongBpmTime(state.JsonTime);
        foreach (var (groups, events) in GetContainers())
        {
            groups.RecomputeTimesAfter(boundary, groupTime);
            events.RecomputeTimesAfter(boundary, eventTime);
        }

        Refresh();
    }

    // Beat Saber processes boxes in serialized order. The first valid box to select a light wins, even if it
    // has no events. Rotation and translation axes are independent. A later group interrupts that light at
    // its distributed start beat.
    public override void InsertData(TGroup data)
    {
        var taken = new HashSet<(Axis, int)>();
        foreach (var box in data.Boxes)
        {
            if (!IsAuthoredBox(box))
                continue;
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            if (indexFilter == null) continue; // i pretend to not see
            var lastEventTime = GetEventCount(box) > 0
                ? GetLastEventTime(box)
                : 0f;
            var beatStep = DistributionHelper.GetBeatStep(
                DistributionHelper.GetDurationCount(indexFilter),
                (DistributionType)box.BeatDistributionType,
                box.BeatDistribution,
                lastEventTime);
            foreach (var entry in indexFilter)
            {
                var (element, durationOrder, distributionOrder) = entry;
                var key = (box.GetAxis(), element);
                var container = GetGroupContainer(key);
                if (!taken.Add(key) || container is null) continue;

                var state = CreateState(data);

                state.StartTime = data.SongBpmTime;
                state.LocalJsonTime = data.JsonTime + (beatStep * durationOrder);

                state.BeatStep = beatStep;
                state.Box = box;

                state.ElementID = element;
                state.DurationOrder = durationOrder;
                state.DistributionOrder = distributionOrder;
                state.AffectedChunkOrder = entry.AffectedChunkOrder;
                state.AffectedChunkCount = indexFilter.VisibleCount;
                state.AffectedLightOrder = entry.AffectedLightOrder;
                state.AffectedLightCount = indexFilter.AffectedLightCount;
                state.ConvertedIndexFilter = indexFilter;

                HandleInsertState(container, state);
            }
        }
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        TGroupState newState,
        TGroupState prevState,
        TGroupState nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);

        RemoveEvents(prevState);

        RegenerateEvents(prevState, newState.LocalJsonTime);
        RegenerateEvents(newState, nextState.LocalJsonTime);
    }

    protected void RegenerateEvents(TGroupState state, float maxRelativeJsonTime)
    {
        var key = (state.Box.GetAxis(), state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        // The claim pass already converted this box's filter; re-converting here is O(lights) per edit.
        var indexFilter = state.ConvertedIndexFilter
            ?? IndexFilterHelper.Convert(state.Box.IndexFilter, Count);
        var distributionOffset = GetDistribution(indexFilter, state.Box, state.DistributionOrder);
        var events = GenerateEvents(state, distributionOffset, maxRelativeJsonTime);
        foreach (var data in events)
            HandleInsertEventState(container, data, out _, out _);
        state.SetEvents(events);
    }

    internal static void HandleInsertEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState newState,
        out TEventState prevState,
        out TEventState nextState)
    {
        prevState = container.GetOverlappingStateFrom(newState);
        nextState = container.GetNextStateFrom(newState);

        prevState.EndTime = newState.StartTime;
        prevState.Next = newState;

        newState.EndTime = nextState.StartTime;
        newState.Previous = prevState;
        if (newState.Previous.UsePrevious) newState.Previous = newState.Previous.Previous;
        newState.Next = nextState;

        nextState.Previous = newState;
        if (nextState.Previous.UsePrevious) nextState.Previous = nextState.Previous.Previous;

        container.AddState(newState);
    }

    public override void RemoveData(
        TGroup reference,
        TGroup original)
    {
        var taken = new HashSet<(Axis, int)>();
        foreach (var box in original.Boxes)
        {
            if (!IsAuthoredBox(box))
                continue;
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            if (indexFilter == null) continue; // i also pretend to not see
            foreach (var (element, _, _) in indexFilter)
            {
                var key = (box.GetAxis(), element);
                var container = GetGroupContainer(key);
                if (!taken.Add(key) || container is null) continue;

                HandleRemoveState(container, reference, original);
            }
        }
    }

    private void RemoveEvents(TGroupState state)
    {
        var key = (state.Box.GetAxis(), state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        foreach (var evt in state.Events)
            HandleRemoveEventState(container, evt as TEventState, out _, out _);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        TGroupState currState,
        TGroupState prevState,
        TGroupState nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);

        RemoveEvents(prevState);
        RemoveEvents(currState);

        RegenerateEvents(prevState, nextState.LocalJsonTime);
    }

    internal static void HandleRemoveEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState stateToRemove,
        out TEventState prevState,
        out TEventState nextState)
    {
        prevState = container.GetPreviousStateFrom(stateToRemove);
        nextState = container.GetNextStateFrom(stateToRemove);

        prevState.EndTime = nextState.StartTime;
        prevState.Next = nextState;

        nextState.Previous = prevState;
        if (nextState.Previous.UsePrevious) nextState.Previous = nextState.Previous.Previous;

        container.RemoveState(stateToRemove);
    }

    protected abstract StateChunksContainer<TGroupState, TGroup> GetGroupContainer((Axis axis, int element) key);
    protected abstract StateChunksContainer<TEventState, TEvent> GetEventContainer((Axis axis, int element) key);

    protected abstract
        IEnumerable<(StateChunksContainer<TGroupState, TGroup> groupContainer, StateChunksContainer<TEventState, TEvent>
            eventContainer)>
        GetContainers();

    protected abstract int GetEventCount(TBox box);
    protected abstract float GetLastEventTime(TBox box);

    protected abstract float GetDistribution(IndexFilterHelper.IndexFilter indexFilter, TBox box, int order);

    protected abstract TEventState[] GenerateEvents(
        TGroupState state,
        float distributionOffset,
        float maxRelativeJsonTime);
}

public abstract class EventGroupStateData<TGroup, TBox, TEvent> : StateData<TGroup>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    public float LocalJsonTime;
    public float BeatStep;

    public int ElementID;
    public int DurationOrder;
    public int DistributionOrder;
    public int AffectedChunkOrder;
    public int AffectedChunkCount;
    public int AffectedLightOrder;
    public int AffectedLightCount;

    public TBox Box;
    // RegenerateEvents runs once per claimed element, so keep the claim-time conversion rather than
    // re-parsing the same box filter for every light.
    public IndexFilterHelper.IndexFilter ConvertedIndexFilter;
    public EventGroupEventStateData<TEvent>[] Events = Array.Empty<EventGroupEventStateData<TEvent>>();

    public void SetEvents(EventGroupEventStateData<TEvent>[] events)
    {
        // Generated beats include the per-element distribution offset, which authored node times do not contain.
        foreach (var evt in events)
            evt.JsonTime = LocalJsonTime + evt.Base.RelativeJsonTime;

        Events = events;
    }

    protected EventGroupStateData(TGroup data) : base(data)
    {
    }
}

public abstract class EventGroupEventStateData<T> : StateData<T> where T : BaseGLSEvent
{
    public EventGroupEventStateData<T> Previous;
    public EventGroupEventStateData<T> Next;

    public readonly EaseType EaseType;
    public readonly bool UsePrevious;
    public float JsonTime;

    protected EventGroupEventStateData(T data, float startTime, int easeType, int usePrevious) : base(data)
    {
        EaseType = (EaseType)easeType;
        UsePrevious = usePrevious == 1;
        StartTime = startTime;
    }
}

public abstract record EventGroupContainer<TGroupState, TEventState, TGroup, TBox, TEvent>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    public readonly StateChunksContainer<TGroupState, TGroup> GroupContainer = new();
    public readonly StateChunksContainer<TEventState, TEvent> EventContainer = new();
}
