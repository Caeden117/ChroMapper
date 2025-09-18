using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostState>
{
    private readonly EventStateChunksContainer<ColorBoostState> stateChunksContainer = new();
    public bool Boost;

    public event Action<bool> OnStateChange;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void UpdateTime(float currentTime)
    {
        var state = GetCurrentState(currentTime, stateChunksContainer.Current, stateChunksContainer.Chunks);

        if (stateChunksContainer.Current == state) return;
        stateChunksContainer.Current = state;
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
        InitializeStates(stateChunksContainer.Chunks);
        foreach (var evt in events) InsertEvent(evt);
        stateChunksContainer.Current = GetStateAt(0, stateChunksContainer.Chunks);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        state.Boost = evt.Value == 1;

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

public class ColorBoostState : BasicEventState
{
    public bool Boost;
}
