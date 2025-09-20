using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class BasicEventManager : MonoBehaviour
{
    public EventPriority Priority;
    public AudioTimeSyncController Atsc;

    public abstract void Initialize();
    public abstract void UpdateTime(float time);
    public abstract void BuildFromEvents(IEnumerable<BaseEvent> events);
    public abstract void InsertEvent(BaseEvent evt);
    public abstract void RemoveEvent(BaseEvent evt);
    public abstract void Reset();
}

public abstract class BasicEventManager<T> : BasicEventManager where T : BasicEventState
{
    protected abstract T CreateState(BaseEvent evt);

    protected EventStateChunksContainer<T> InitializeStates(EventStateChunksContainer<T> container)
    {
        container.GenerateChunk(Atsc);

        var start = CreateState(new BaseEvent());
        var end = CreateState(new BaseEvent());
        end.StartTime = end.EndTime;
        container.Chunks[0].Add(start);
        container.Chunks[^1].Add(end);

        container.SetStateAt(0);
        return container;
    }

    protected void HandleInsertState(EventStateChunksContainer<T> container, T newState)
    {
        var (prevChunk, prevIndex, prevState) = container.GetOverlappingStateFrom(newState);
        var (nextChunk, _, nextState) = container.GetNextStateFrom(newState);

        OnInsertUpdateToPreviousState(newState, prevState);
        OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        OnInsertUpdateFromNextState(newState, nextState);
        OnInsertUpdateToNextState(newState, nextState);

        var (_, chunk) = container.GetChunk(newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
            chunk.Insert(prevIndex + 1, newState);
    }

    protected virtual void OnInsertUpdateToPreviousState(T newState, T prevState) =>
        prevState.EndTime = newState.StartTime;

    protected virtual void OnInsertUpdateFromNextState(T newState, T nextState) =>
        newState.EndTime = nextState.StartTime;

    protected virtual void OnInsertUpdateToNextState(T newState, T prevState) { }

    protected virtual void OnInsertUpdateFromPreviousStateAndNextState(T newState, T prevState, T nextState) { }

    protected virtual void OnInsertConsequentUpdateToNextState(T newState, T nextState) { }

    protected void HandleInsertUpdateConsequentStateFrom(EventStateChunksContainer<T> container, T currState)
    {
        var enumerator = container.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnInsertConsequentUpdateToNextState(currState, nextState);
        }
    }

    protected T HandleRemoveState(EventStateChunksContainer<T> container, T stateToRemove)
    {
        var (_, currChunk) = container.GetChunk(stateToRemove.StartTime);
        var (_, _, prevState) = container.GetPreviousStateFrom(stateToRemove);
        var (_, _, nextState) = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextState(stateToRemove, prevState, nextState);
        currChunk.Remove(stateToRemove);

        return stateToRemove;
    }

    protected T HandleRemoveState(EventStateChunksContainer<T> container, BaseEvent evt)
    {
        var (_, _, state) = container.GetStateFrom(evt);
        return HandleRemoveState(container, state);
    }

    protected virtual void OnRemoveUpdateToNextState(T currState, T nextState) { }

    protected void HandleRemoveUpdateConsequentStateFrom(EventStateChunksContainer<T> container, T currState)
    {
        var enumerator = container.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnRemoveUpdateToNextState(currState, nextState);
        }
    }

    protected virtual void
        OnRemoveUpdatePreviousAndNextState(T currState, T prevState, T nextState) =>
        prevState.EndTime = nextState.StartTime;
}

public enum EventPriority
{
    ColorBoost,
    SpecialFX,
    Translation,
    Rotation,
    Light,
}

public abstract class BasicEventState : IEquatable<BasicEventState>
{
    private static int ID;
    private readonly int id = ID++; // maybe reference equality is better, idk

    protected BasicEventState(BaseEvent evt) => BaseEvent = evt;

    public readonly BaseEvent BaseEvent;
    public float StartTime = float.MinValue;
    public float EndTime = float.MaxValue;

    public bool Equals(BasicEventState other) => id == other!.id;
}
