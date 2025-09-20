using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Object = UnityEngine.Object;

public abstract class TrackLaneRingsManagerBase : BasicEventManager<RingRotationState>
{
    public RingFilter RingFilter;
    private readonly Dictionary<int, EventStateChunksContainer<RingRotationState>> stateChunksContainerMap = new();

    public abstract void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index);
    public abstract void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index);
    public virtual float GetInitialRotation() => -45f;
    public virtual float GetRotationStep() => 0f;
    public virtual bool GetDirection() => false;

    public abstract Object[] GetToDestroy();

    public override void Initialize()
    {
        stateChunksContainerMap.Clear();
        foreach (var type in new List<int> { 8, 9 }.Where(type => !stateChunksContainerMap.ContainsKey(type)))
        {
            stateChunksContainerMap[type] = InitializeStates(new EventStateChunksContainer<RingRotationState>());
            foreach (var state in stateChunksContainerMap[type].Chunks.SelectMany(chunk => chunk))
                state.BaseEvent.Type = type;
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var container in stateChunksContainerMap.Values)
        {
            var previousState = container.CurrentState;
            container.SetCurrentState(currentTime, Atsc.IsPlaying);
            if (container.CurrentState == previousState) continue;
            UpdateObject(container.CurrentState);
        }
    }

    private void UpdateObject(RingRotationState state)
    {
        var evt = state.BaseEvent;
        var index = stateChunksContainerMap[state.BaseEvent.Type].GetStateIndex(state);
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
        new(evt) { RotationInitial = GetInitialRotation(), RotationChange = GetRotationStep() };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertEvent(evt);
    }

    protected override void OnInsertUpdateToPreviousState(
        RingRotationState newState,
        RingRotationState previousState)
    {
        base.OnInsertUpdateToPreviousState(newState, previousState);
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

        var container = stateChunksContainerMap[evt.Type];
        HandleInsertState(container, state);
        HandleInsertUpdateConsequentStateFrom(container, state);
    }

    protected override void OnInsertConsequentUpdateToNextState(
        RingRotationState currState,
        RingRotationState nextState) =>
        nextState.RotationInitial += currState.RotationChange;

    public override void RemoveEvent(BaseEvent evt)
    {
        var container = stateChunksContainerMap[evt.Type];
        var (_, _, state) = container.GetStateFrom(evt);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(evt.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    protected override void OnRemoveUpdateToNextState(
        RingRotationState currState,
        RingRotationState nextState)
    {
        base.OnRemoveUpdateToNextState(currState, nextState);
        nextState.RotationInitial -= currState.RotationChange;
    }

    public override void Reset()
    {
        foreach (var ringType in stateChunksContainerMap.Values) UpdateObject(ringType.CurrentState);
    }
}

public class RingRotationState : BasicEventState
{
    // unfortunately, you cannot modulo this out, so there's a chance this can overflow
    public float RotationInitial;
    public float RotationChange;
    public bool Direction;

    public RingRotationState(BaseEvent evt) : base(evt)
    {
    }
}

public enum RingFilter
{
    Big,
    Small
}
