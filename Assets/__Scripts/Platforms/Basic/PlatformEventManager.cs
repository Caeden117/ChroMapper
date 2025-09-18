using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public abstract class PlatformEventManager : BasicEventManager<PlatformEventState>
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, BaseEvent evt);

    private readonly Dictionary<int, EventStateChunksContainer<PlatformEventState>> stateChunksContainerMap = new();

    public override void Initialize() => stateChunksContainerMap.Clear();

    public override void UpdateTime(float currentTime)
    {
        foreach (var container in stateChunksContainerMap.Values)
        {
            var state = GetCurrentState(currentTime, container.Current, container.Chunks);
            if (container.Current == state) continue;
            container.Current = state;
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
        if (!stateChunksContainerMap.ContainsKey(type))
        {
            var container = new EventStateChunksContainer<PlatformEventState>();
            InitializeStates(container.Chunks);
            foreach (var state in container.Chunks.SelectMany(state => state)) state.BaseEvent.Type = type;
            container.Current = GetStateAt(0, container.Chunks);
            stateChunksContainerMap[type] = container;
        }

        foreach (var evt in baseEvents) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, stateChunksContainerMap[evt.Type].Chunks);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var container = stateChunksContainerMap[evt.Type];
        var state = RemoveState(evt, container.Chunks);
        if (container.Current != state) return;
        container.Current = GetStateAt(evt.SongBpmTime, container.Chunks);
        UpdateObject(container.Current);
    }

    public override void Reset()
    {
        foreach (var container in stateChunksContainerMap.Values) UpdateObject(container.Current);
    }
}

public class PlatformEventState : BasicEventState
{
}
