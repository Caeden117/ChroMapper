using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSProvider : StateManager<VariableNJSStateData, BaseNJSEvent>
{
    [Header("State")] public float NoteJumpSpeed;

    public float JumpDuration;
    public float JumpDistance;

    public float HalfJumpDurationInBeats;
    public float HalfJumpDuration;
    public float HalfJumpDistance;

    public float EditorScale;

    [Header("Cached")] public float BaseBeatPerMinute;
    public float BaseNoteJumpSpeed;
    public float BaseHalfJumpDurationInBeats;
    public float OneBeatDuration;

    private readonly Dictionary<int, int> hjds = new();
    public float MaxHalfJumpDurationInBeats;
    private readonly SortedList<float, int> spawnAheadTimes = new();
    public float SpawnAheadTime { get; private set; }
    internal event Action SpawnAheadTimeChanged;

    public event Action OnChanged;

    private readonly StateChunksContainer<VariableNJSStateData, BaseNJSEvent> container = new();

    public override void Initialize()
    {
        BaseBeatPerMinute = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
        BaseNoteJumpSpeed = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        var offset = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteStartBeatOffset;
        hjds.Clear();
        spawnAheadTimes.Clear();

        OneBeatDuration = 60f / BaseBeatPerMinute;
        MaxHalfJumpDurationInBeats = BaseHalfJumpDurationInBeats = SpawnParameterHelper.CalculateHalfJumpDuration(
            BaseNoteJumpSpeed,
            offset,
            BaseBeatPerMinute);
        // Reloading a difficulty can keep NJS while changing BPM or jump offset.
        NoteJumpSpeed = BaseNoteJumpSpeed;
        UpdateMovementValues();

        InitializeStates(
            container,
            CreateState(new BaseNJSEvent { UsePrevious = 1 }),
            CreateState(new BaseNJSEvent { UsePrevious = 1 }));
        InsertData(new BaseNJSEvent());
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        container.IsCurrentOrFindState(time, Atsc.IsPlaying);

        var currentState = container.CurrentState;
        var normalizedTime = (time - currentState.StartTime) / (currentState.EndTime - currentState.StartTime);
        var njs = Mathf.Max(
            BaseNoteJumpSpeed
            + Mathf.Lerp(
                currentState.RelativeNjs,
                currentState.NextRelativeNjs,
                currentState.Easing(normalizedTime)),
            0.01f);

        if (Mathf.Approximately(njs, NoteJumpSpeed)) return;
        NoteJumpSpeed = njs;
        UpdateState();
    }

    public void UpdateState()
    {
        UpdateMovementValues();
        OnChanged?.Invoke();
    }

    private void UpdateMovementValues()
    {
        EditorScale = 100f * NoteJumpSpeed / BaseBeatPerMinute;

        var factor = Mathf.Min(NoteJumpSpeed / BaseNoteJumpSpeed, 1f);
        HalfJumpDuration = OneBeatDuration * BaseHalfJumpDurationInBeats / factor;
        HalfJumpDurationInBeats = Atsc.GetBeatFromSeconds(HalfJumpDuration);
        JumpDuration = HalfJumpDuration * 2f;

        JumpDistance = NoteJumpSpeed * JumpDuration;
        HalfJumpDistance = JumpDistance * 0.5f;
    }

    protected override void OnInsertUpdateToPreviousState(VariableNJSStateData newState, VariableNJSStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.NextRelativeNjs = newState.Base.UsePrevious == 1 ? prevState.RelativeNjs : newState.RelativeNjs;
        var easingId = newState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => newState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    protected override void OnInsertUpdateToNextState(VariableNJSStateData newState, VariableNJSStateData nextState)
    {
        base.OnInsertUpdateToNextState(newState, nextState);
        nextState.RelativeNjs = nextState.Base.UsePrevious == 1 ? newState.RelativeNjs : nextState.RelativeNjs;
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        VariableNJSStateData newState,
        VariableNJSStateData prevState,
        VariableNJSStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        newState.RelativeNjs = newState.Base.UsePrevious == 1 ? prevState.RelativeNjs : newState.RelativeNjs;
        newState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? newState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        newState.Easing = Easing.FromID(easingId);
    }

    public override void InsertData(BaseNJSEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.RelativeNjs = data.UsePrevious == 1 ? 0 : data.RelativeNJS;

        // Native callbacks use the exact longest spawn lead, while editor chunks round their half jumps.
        var spawnAheadTime = GetSpawnAheadTime(data);
        spawnAheadTimes.TryGetValue(spawnAheadTime, out var count);
        spawnAheadTimes[spawnAheadTime] = count + 1;
        RefreshSpawnAheadTime();

        var factor = Mathf.Min((BaseNoteJumpSpeed + state.RelativeNjs) / BaseNoteJumpSpeed, 1f);
        var hjd = OneBeatDuration * BaseHalfJumpDurationInBeats / factor;
        var hjdInBeat = Mathf.CeilToInt(Atsc.GetBeatFromSeconds(hjd));

        if (hjds.TryAdd(hjdInBeat, 0)) MaxHalfJumpDurationInBeats = hjds.Keys.Max();
        hjds[hjdInBeat]++;

        HandleInsertState(container, state);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        VariableNJSStateData currState,
        VariableNJSStateData prevState,
        VariableNJSStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? prevState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    public override void RemoveData(BaseNJSEvent reference, BaseNJSEvent original)
    {
        var state = HandleRemoveState(container, reference, original);
        if (state == container.CurrentState) container.SetStateAt(reference.SongBpmTime);

        var spawnAheadTime = GetSpawnAheadTime(original);
        if (spawnAheadTimes[spawnAheadTime] == 1)
            spawnAheadTimes.Remove(spawnAheadTime);

        else
            spawnAheadTimes[spawnAheadTime]--;

        RefreshSpawnAheadTime();

        var factor = Mathf.Min((BaseNoteJumpSpeed + state.RelativeNjs) / BaseNoteJumpSpeed, 1f);
        var hjd = OneBeatDuration * BaseHalfJumpDurationInBeats / factor;
        var hjdInBeat = Mathf.CeilToInt(Atsc.GetBeatFromSeconds(hjd));

        if (!hjds.ContainsKey(hjdInBeat)) return;
        hjds[hjdInBeat]--;

        if (hjds[hjdInBeat] != 0) return;
        hjds.Remove(hjdInBeat);
        MaxHalfJumpDurationInBeats = hjds.Keys.Max();
    }

    public override void Refresh() => UpdateState();

    private float GetSpawnAheadTime(BaseNJSEvent data)
    {
        var relativeNjs = data.UsePrevious == 1
            ? 0f
            : data.RelativeNJS;
        var factor = Mathf.Min((BaseNoteJumpSpeed + relativeNjs) / BaseNoteJumpSpeed, 1f);
        return 0.5f + (OneBeatDuration * BaseHalfJumpDurationInBeats / factor);
    }

    private void RefreshSpawnAheadTime()
    {
        var value = spawnAheadTimes.Keys[spawnAheadTimes.Count - 1];
        if (SpawnAheadTime == value)
            return;

        SpawnAheadTime = value;
        SpawnAheadTimeChanged?.Invoke();
    }

    protected override VariableNJSStateData CreateState(BaseNJSEvent data) => new(data);
}

public class VariableNJSStateData : StateData<BaseNJSEvent>
{
    public Func<float, float> Easing = global::Easing.Linear;
    public float RelativeNjs;
    public float NextRelativeNjs;

    public VariableNJSStateData(BaseNJSEvent @base) : base(@base)
    {
    }
}
