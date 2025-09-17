using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class RotatingLightsManagerBase : BasicEventManager<RotatingLightState>
{
    public abstract void UpdateOffset(bool isLeftEvent, BaseEvent evt);

    public abstract bool IsOverrideLightGroup();
    public readonly List<List<RotatingLightState>> StateChunks = new();
    public RotatingLightState CurrentState;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var state = GetCurrentState(currentTime, CurrentState, StateChunks);

        if (CurrentState == state) return;
        CurrentState = state;
        UpdateObject(state);
    }

    private void UpdateObject(RotatingLightState state) => UpdateOffset(true, state.BaseEvent);

    protected override RotatingLightState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        InitializeStates(StateChunks);
        CurrentState = GetStateAt(0, StateChunks);
        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, StateChunks);
    }

    public override void RemoveEvent(BaseEvent evt) => RemoveState(evt, StateChunks);

    public override void Reset() => UpdateObject(CurrentState);
}

public class RotatingLightState : BasicEventState
{
}
