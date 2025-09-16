using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostState>
{
    public readonly List<ColorBoostState> States = new();
    public int CurrentIndex;
    public bool Boost;

    public event Action<bool> OnStateChange;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var (idx, state) = GetCurrentState(currentTime, CurrentIndex, States);
        
        if (CurrentIndex == idx) return;
        CurrentIndex = idx;
        UpdateObject(state);
    }

    private void UpdateObject(ColorBoostState state)
    {
        if (state.Boost == Boost) return;
        Boost = state.Boost;
        OnStateChange(Boost);
    }

    protected override ColorBoostState CreateState(BaseEvent evt) =>
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
        state.Boost = evt.Value == 1;

        InsertState(state, States);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = States.Find(s => s.BaseEvent == evt);
        RemoveState(state, States);
    }

    public override void Reset() => UpdateObject(States[CurrentIndex]);
}

public struct ColorBoostState : IBasicEventState
{
    public BaseEvent BaseEvent { get; set; }

    public float StartTime { get; set; }
    public float EndTime { get; set; }
    public bool Boost;
}
