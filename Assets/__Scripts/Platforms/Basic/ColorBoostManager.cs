using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostState>
{
    private readonly EventStateChunksContainer<ColorBoostState> stateChunksContainer = new();
    public bool Boost;

    public event Action<bool> OnStateChange;

    public void Awake() => Priority = EventPriority.ColorBoost;

    public override void Initialize() => InitializeStates(stateChunksContainer);

    public override void UpdateTime(float currentTime)
    {
        var previousState = stateChunksContainer.CurrentState;
        SetCurrentState(currentTime, Atsc.IsPlaying, stateChunksContainer);
        if (stateChunksContainer.CurrentState == previousState) return;
        UpdateObject(stateChunksContainer.CurrentState);
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
        foreach (var evt in events) InsertEvent(evt);
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
        if (stateChunksContainer.CurrentState != state) return;
        SetStateAt(evt.SongBpmTime, stateChunksContainer);
        UpdateObject(stateChunksContainer.CurrentState);
    }

    public override void Reset() => UpdateObject(stateChunksContainer.CurrentState);
}

public class ColorBoostState : BasicEventState
{
    public bool Boost;
}
