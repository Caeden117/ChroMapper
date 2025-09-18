using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Object = UnityEngine.Object;

public abstract class TrackLaneRingsManagerBase : BasicEventManager<RingRotationState>
{
    public RingFilter RingFilter;
    private readonly Dictionary<int, List<List<RingRotationState>>> ringTypeStateChunksMap = new();

    private readonly Dictionary<int, RingRotationState> ringTypeCurrentMap =
        new();

    public abstract void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index);
    public abstract void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index);
    public virtual float GetInitialRotation() => -45f;
    public virtual float GetRotationStep() => 0f;
    public virtual bool GetDirection() => false;

    public abstract Object[] GetToDestroy();

    public override void UpdateTime(float currentTime)
    {
        foreach (var (type, states) in ringTypeStateChunksMap)
        {
            var currentState = ringTypeCurrentMap[type];
            var state = GetCurrentState(currentTime, currentState, states);

            if (currentState == state) continue;
            ringTypeCurrentMap[type] = state;
            UpdateObject(state);
        }
    }

    private void UpdateObject(RingRotationState state)
    {
        var evt = state.BaseEvent;
        var index = GetStateIndex(state, ringTypeStateChunksMap[state.BaseEvent.Type]);
        switch (evt.Type)
        {
            case 8:
                if (evt.CustomNameFilter != null)
                {
                    var filter = evt.CustomNameFilter;
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        if (RingFilter == RingFilter.Big) HandleRotationEvent(state, evt, index);
                    }
                    else if (filter.Contains("Small") || filter.Contains("Panels") || filter.Contains("Triangle"))
                    {
                        if (RingFilter == RingFilter.Small) HandleRotationEvent(state, evt, index);
                    }
                    else
                        HandleRotationEvent(state, evt, index);
                }
                else
                    HandleRotationEvent(state, evt, index);

                break;
            case 9:
                HandlePositionEvent(state, evt, index);
                break;
        }
    }

    protected override RingRotationState CreateState(BaseEvent evt) =>
        new()
        {
            BaseEvent = evt,
            StartTime = float.MinValue,
            EndTime = float.MaxValue,
            RotationInitial = GetInitialRotation(),
            RotationChange = GetRotationStep()
        };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var type in new List<int> { 8, 9 }.Where(type => !ringTypeStateChunksMap.ContainsKey(type)))
        {
            ringTypeStateChunksMap[type] = new();
            InitializeStates(ringTypeStateChunksMap[type]);
            foreach (var state in ringTypeStateChunksMap[type].SelectMany(chunk => chunk)) state.BaseEvent.Type = type;
            ringTypeCurrentMap[type] = GetStateAt(0, ringTypeStateChunksMap[type]);
        }

        foreach (var evt in events) InsertEvent(evt);
    }

    protected override void UpdateToPreviousStateOnInsert(
        RingRotationState newState,
        RingRotationState previousState)
    {
        base.UpdateToPreviousStateOnInsert(newState, previousState);
        newState.RotationInitial = previousState.RotationInitial + previousState.RotationChange;
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        state.RotationChange = evt.CustomRingRotation ?? GetRotationStep();
        state.Direction = GetDirection();
        if (evt.CustomData != null) state.Direction = evt.CustomDirection == 0;
        state.RotationChange = state.Direction ? state.RotationChange : -state.RotationChange;

        InsertState(state, ringTypeStateChunksMap[evt.Type]);
        UpdateConsequentStateAfterInsertFrom(state, ringTypeStateChunksMap[evt.Type]);
    }

    protected override RingRotationState UpdateToNextStateOnInsert(
        RingRotationState currState,
        RingRotationState nextState)
    {
        nextState = base.UpdateToNextStateOnInsert(currState, nextState);
        nextState.RotationInitial += currState.RotationChange;
        return nextState;
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = GetStateFrom(evt, ringTypeStateChunksMap[evt.Type]);
        UpdateConsequentStateBeforeRemoveFrom(state, ringTypeStateChunksMap[evt.Type]);
        RemoveState(state, ringTypeStateChunksMap[evt.Type]);

        if (ringTypeCurrentMap[evt.Type] != state) return;
        ringTypeCurrentMap[evt.Type] = GetStateAt(evt.SongBpmTime, ringTypeStateChunksMap[evt.Type]);
        UpdateObject(ringTypeCurrentMap[evt.Type]);
    }

    protected override RingRotationState UpdateToNextStateOnRemove(
        RingRotationState currState,
        RingRotationState nextState)
    {
        nextState = base.UpdateToNextStateOnRemove(currState, nextState);
        nextState.RotationInitial -= currState.RotationChange;
        return nextState;
    }

    public override void Reset()
    {
        foreach (var state in ringTypeCurrentMap.Values) UpdateObject(state);
    }
}

public class RingRotationState : BasicEventState
{
    // unfortunately, you cannot modulo this out, so there's a chance this can overflow
    public float RotationInitial;
    public float RotationChange;
    public bool Direction;
}

public enum RingFilter
{
    Big,
    Small
}
