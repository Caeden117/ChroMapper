using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public abstract class PlatformEventHandler : BasicEventManager<PlatformEventState>
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, BaseEvent evt);

    private readonly Dictionary<int, List<PlatformEventState>> platformTypeStatesMap = new();
    private readonly Dictionary<int, int> platformTypeIndexMap = new();

    public override void UpdateTime(float currentTime)
    {
        foreach (var (type, states) in platformTypeStatesMap)
        {
            var currentIndex = platformTypeIndexMap[type];
            var (idx, state) = GetCurrentState(currentTime, currentIndex, states);

            if (currentIndex == idx) continue;
            platformTypeIndexMap[type] = idx;
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
        if (platformTypeIndexMap.TryAdd(type, 0))
        {
            platformTypeStatesMap[type] = new List<PlatformEventState>();
            InitializeStates(platformTypeStatesMap[type]);
            foreach (var state in platformTypeStatesMap[type]) state.BaseEvent.Type = type;
        }

        foreach (var evt in baseEvents) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, platformTypeStatesMap[evt.Type]);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = platformTypeStatesMap[evt.Type].Find(s => s.BaseEvent == evt);
        RemoveState(state, platformTypeStatesMap[evt.Type]);
    }

    public override void Reset()
    {
        foreach (var (type, states) in platformTypeStatesMap)
        {
            var index = platformTypeIndexMap[type];
            UpdateObject(states[index]);
        }
    }
}

public struct PlatformEventState : IBasicEventState
{
    public BaseEvent BaseEvent { get; set; }
    public float StartTime { get; set; }
    public float EndTime { get; set; }
}
