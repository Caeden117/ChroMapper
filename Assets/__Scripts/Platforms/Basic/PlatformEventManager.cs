using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public abstract class PlatformEventManager : BasicEventManager<PlatformEventState>
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, BaseEvent evt);

    private readonly Dictionary<int, List<List<PlatformEventState>>> platformTypeStateChunksMap = new();
    private readonly Dictionary<int, PlatformEventState> platformTypeCurrentMap = new();

    public override void UpdateTime(float currentTime)
    {
        foreach (var (type, states) in platformTypeStateChunksMap)
        {
            var currentState = platformTypeCurrentMap[type];
            var state = GetCurrentState(currentTime, currentState, states);

            if (currentState == state) continue;
            platformTypeCurrentMap[type] = state;
            UpdateObject(state);
        }
    }

    private void UpdateObject(PlatformEventState state)
    {
        var evt = state.BaseEvent;
        OnEventTrigger(evt.Type, evt);
    }

    protected override PlatformEventState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        var baseEvents = events.ToList();
        var type = baseEvents.First().Type;
        if (!platformTypeCurrentMap.ContainsKey(type))
        {
            platformTypeStateChunksMap[type] = new List<List<PlatformEventState>>();
            InitializeStates(platformTypeStateChunksMap[type]);
            foreach (var state in platformTypeStateChunksMap[type].SelectMany(state => state))
                state.BaseEvent.Type = type;
            platformTypeCurrentMap[type] = GetStateAt(0, platformTypeStateChunksMap[type]);
        }

        foreach (var evt in baseEvents) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, platformTypeStateChunksMap[evt.Type]);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = RemoveState(evt, platformTypeStateChunksMap[evt.Type]);
        if (platformTypeCurrentMap[evt.Type] == state)
            platformTypeCurrentMap[evt.Type] = GetStateAt(evt.SongBpmTime, platformTypeStateChunksMap[evt.Type]);
    }

    public override void Reset()
    {
        foreach (var states in platformTypeCurrentMap.Values) UpdateObject(states);
    }
}

public class PlatformEventState : BasicEventState
{
}
