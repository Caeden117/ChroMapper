using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostState>
{
    public readonly List<List<ColorBoostState>> StateChunks = new();
    public ColorBoostState CurrentState;
    public bool Boost;

    public event Action<bool> OnStateChange;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var state = GetCurrentState(currentTime, CurrentState, StateChunks);

        if (CurrentState == state) return;
        CurrentState = state;
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
        InitializeStates(StateChunks);
        foreach (var evt in events) InsertEvent(evt);
        CurrentState = GetStateAt(0, StateChunks);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        state.Boost = evt.Value == 1;

        InsertState(state, StateChunks);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = RemoveState(evt, StateChunks);
        if (CurrentState == state) CurrentState = GetStateAt(evt.SongBpmTime, StateChunks);
    }

    public override void Reset() => UpdateObject(CurrentState);
}

public class ColorBoostState : BasicEventState
{
    public bool Boost;
}
