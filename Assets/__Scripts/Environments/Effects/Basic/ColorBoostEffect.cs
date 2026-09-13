using System;
using Beatmap.Base;

public class ColorBoostEffect : BasicEventEffect<ColorBoostStateData>, IEffectStateSignal<bool>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> container = new();
    public ColorSchemeProvider ColorSchemeProvider;
    public bool Boost;

    public event Action<bool> OnStateChanged;

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(ColorBoostStateData stateData)
    {
        if (stateData.Boost == Boost) return;
        Boost = stateData.Boost;
        ColorSchemeProvider.ColorScheme.SwapEnvironmentColors(Boost);
        OnStateChanged?.Invoke(Boost);
    }

    public bool GetCurrentState() => Boost;
    
    protected override ColorBoostStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.Boost = data.Value == 1;

        HandleInsertState(container, state);

        // Inserting a boost before the paused preview time must replace the cached current state immediately.
        if (state.StartTime <= Atsc.CurrentSongBpmTime)
        {
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = HandleRemoveState(container, reference, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
        UpdateObject(container.CurrentState);
    }
}

public class ColorBoostStateData : BasicEventStateData
{
    public bool Boost;

    public ColorBoostStateData(BaseEvent data) : base(data)
    {
    }
}
