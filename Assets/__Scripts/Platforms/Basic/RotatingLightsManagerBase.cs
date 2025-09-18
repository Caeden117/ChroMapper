using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class RotatingLightsManagerBase : BasicEventManager<RotatingLightState>
{
    public abstract void UpdateOffset(bool isLeftEvent, BaseEvent evt);

    public abstract bool IsOverrideLightGroup();
    private readonly EventStateChunksContainer<RotatingLightState> stateChunksContainer = new();

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var state = GetCurrentState(currentTime, stateChunksContainer.Current, stateChunksContainer.Chunks);

        if (stateChunksContainer.Current == state) return;
        stateChunksContainer.Current = state;
        UpdateObject(state);
    }

    private void UpdateObject(RotatingLightState state) => UpdateOffset(true, state.BaseEvent);

    protected override RotatingLightState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        InitializeStates(stateChunksContainer.Chunks);
        stateChunksContainer.Current = GetStateAt(0, stateChunksContainer.Chunks);
        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, stateChunksContainer.Chunks);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = RemoveState(evt, stateChunksContainer.Chunks);
        if (stateChunksContainer.Current != state) return;
        stateChunksContainer.Current = GetStateAt(evt.SongBpmTime, stateChunksContainer.Chunks);
        UpdateObject(stateChunksContainer.Current);
    }

    public override void Reset() => UpdateObject(stateChunksContainer.Current);
}

public class RotatingLightState : BasicEventState
{
}
