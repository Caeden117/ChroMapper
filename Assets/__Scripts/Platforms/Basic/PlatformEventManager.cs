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
            var previousState = container.CurrentState;
            container.SetCurrentState(currentTime, Atsc.IsPlaying);
            if (container.CurrentState == previousState) continue;
            UpdateObject(container.CurrentState);
        }
    }

    private void UpdateObject(PlatformEventState state)
    {
        var evt = state.BaseEvent;
        OnEventTrigger(evt.Type, evt);
    }

    protected override PlatformEventState CreateState(BaseEvent evt) => new(evt);

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        var baseEvents = events.ToList();
        var type = baseEvents.First().Type;
        if (!stateChunksContainerMap.ContainsKey(type))
        {
            stateChunksContainerMap[type] = InitializeStates(new EventStateChunksContainer<PlatformEventState>());
            foreach (var state in stateChunksContainerMap[type].Chunks.SelectMany(state => state))
                state.BaseEvent.Type = type;
        }

        foreach (var evt in baseEvents) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        HandleInsertState(stateChunksContainerMap[evt.Type], state);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var container = stateChunksContainerMap[evt.Type];
        var state = HandleRemoveState(container, evt);
        if (container.CurrentState != state) return;
        container.SetStateAt(evt.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void Reset()
    {
        foreach (var container in stateChunksContainerMap.Values) UpdateObject(container.CurrentState);
    }
}

public class PlatformEventState : BasicEventState
{
    public PlatformEventState(BaseEvent evt) : base(evt)
    {
    }
}
