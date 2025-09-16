using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class RotatingLightsManagerBase : BasicEventManager<RotatingLightState>
{
    public abstract void UpdateOffset(bool isLeftEvent, BaseEvent evt);

    public abstract bool IsOverrideLightGroup();
    public readonly List<RotatingLightState> States = new();
    public int CurrentIndex;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var (idx, state) = GetCurrentState(currentTime, CurrentIndex, States);
        
        if (CurrentIndex == idx) return;
        CurrentIndex = idx;
        UpdateObject(state);
    }

    private void UpdateObject(RotatingLightState state) => UpdateOffset(true, state.BaseEvent);

    protected override RotatingLightState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        CurrentIndex = 0;
        InitializeStates(States);
        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, States);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = States.Find(s => s.BaseEvent == evt);
        RemoveState(state, States);
    }

    public override void Reset() => UpdateObject(States[CurrentIndex]);
}

public struct RotatingLightState : IBasicEventState
{
    public BaseEvent BaseEvent { get; set; }
    public float StartTime { get; set; }
    public float EndTime { get; set; }
}
