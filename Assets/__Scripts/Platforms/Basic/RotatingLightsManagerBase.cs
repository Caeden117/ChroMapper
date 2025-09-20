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

    public override void Initialize() => InitializeStates(stateChunksContainer);

    public override void UpdateTime(float currentTime)
    {
        var previousState = stateChunksContainer.CurrentState;
        stateChunksContainer.SetCurrentState(currentTime, Atsc.IsPlaying);
        if (stateChunksContainer.CurrentState == previousState) return;
        UpdateObject(stateChunksContainer.CurrentState);
    }

    private void UpdateObject(RotatingLightState state) => UpdateOffset(true, state.BaseEvent);

    protected override RotatingLightState CreateState(BaseEvent evt) => new(evt);

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        HandleInsertState(stateChunksContainer, state);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = HandleRemoveState(stateChunksContainer, evt);
        if (stateChunksContainer.CurrentState != state) return;
        stateChunksContainer.SetStateAt(evt.SongBpmTime);
        UpdateObject(stateChunksContainer.CurrentState);
    }

    public override void Reset() => UpdateObject(stateChunksContainer.CurrentState);
}

public class RotatingLightState : BasicEventState
{
    public RotatingLightState(BaseEvent evt) : base(evt)
    {
    }
}
