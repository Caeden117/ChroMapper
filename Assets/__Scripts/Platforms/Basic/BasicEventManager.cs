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
    private const float chunkByTime = 10f;

    private static (int index, List<T> chunk) GetChunk(List<List<T>> chunks, float time)
    {
        var index = Mathf.Clamp(Mathf.FloorToInt(time / chunkByTime), 0, chunks.Count - 1);
        return (index, chunks[index]);
    }

    protected abstract T CreateState(BaseEvent evt);

    protected EventStateChunksContainer<T> InitializeStates(EventStateChunksContainer<T> container)
    {
        container.Chunks.Clear();
        for (var time = 0f; time < Atsc.SongAudioSource.clip.length; time += Atsc.GetSecondsFromBeat(chunkByTime))
            container.Chunks.Add(new List<T>());

        var start = CreateState(new BaseEvent());
        var end = CreateState(new BaseEvent());
        end.StartTime = end.EndTime;
        container.Chunks[0].Add(start);
        container.Chunks[^1].Add(end);

        SetStateAt(0, container);
        return container;
    }

    protected void SetCurrentState(float time, bool playing, EventStateChunksContainer<T> container)
    {
        if (playing)
            UseCurrentOrNextState(time, container);
        else
            UseCurrentOrFindState(time, container);
    }

    private static void UseCurrentOrNextState(float time, EventStateChunksContainer<T> container)
    {
        if (time < container.CurrentState.EndTime) return;
        SetNextState(time, container);
    }

    private static void SetNextState(float time, EventStateChunksContainer<T> container)
    {
        while (container.CurrentChunkIndex < container.Chunks.Count)
        {
            var chunk = container.Chunks[container.CurrentChunkIndex];
            while (container.CurrentLocalIndex < chunk.Count)
            {
                container.CurrentState = chunk[container.CurrentLocalIndex];
                if (container.CurrentState.StartTime <= time && time < container.CurrentState.EndTime) return;
                container.CurrentLocalIndex++;
            }

            container.CurrentLocalIndex = 0;
            container.CurrentChunkIndex++;
        }
    }

    private static void UseCurrentOrFindState(float time, EventStateChunksContainer<T> container)
    {
        if (container.CurrentState.StartTime <= time && time < container.CurrentState.EndTime) return;
        SetStateAt(time, container);
    }

    protected static void SetStateAt(float time, EventStateChunksContainer<T> container)
    {
        var (chunkIndex, localIndex, state) = GetStateAt(time, container.Chunks);
        container.CurrentChunkIndex = chunkIndex;
        container.CurrentLocalIndex = localIndex;
        container.CurrentState = state;
    }

    // could be faster with bsearch shenanigan
    private static (int chunkIndex, int localIndex, T state) GetStateAt(float time, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, time);
        var idx = BinarySearch(chunk, time);

        if (idx == -1)
        {
            chunkIdx--;
            while (chunkIdx >= 0)
            {
                chunk = chunks[chunkIdx];
                idx = BinarySearch(chunk, time);

                if (idx != -1) break;
                chunkIdx--;
            }

            // if for whatever reason we cannot find it at all at the first chunk, just default to first.
            if (idx == -1) idx = 0;
        }

        return (chunkIdx, idx, chunk[idx]);
    }

    protected static (int chunkIndex, int localIndex, T state) GetStateFrom(BaseEvent evt, List<List<T>> chunks) =>
        GetStateAt(evt.SongBpmTime, chunks);

    protected static int GetStateIndex(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = chunk.IndexOf(state);
        for (var i = 0; i < chunkIdx; i++) idx += chunks[i].Count;
        return idx;
    }

    private static (List<T> chunk, int index, T state) GetPreviousStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime) - 1;

        if (idx < 0)
        {
            while (--chunkIdx >= 0)
            {
                chunk = chunks[chunkIdx];
                if (chunks[chunkIdx].Count != 0) break;
            }

            idx = chunk.Count - 1;
        }

        return (chunk, idx, chunk[idx]);
    }

    private static (List<T> chunk, int index, T state) GetOverlappingStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime);

        if (idx < 0)
        {
            while (--chunkIdx >= 0)
            {
                chunk = chunks[chunkIdx];
                if (chunks[chunkIdx].Count != 0) break;
            }

            idx = chunk.Count - 1;
        }

        return (chunk, idx, chunk[idx]);
    }

    private static (List<T> chunk, int index, T state) GetNextStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime) + 1;

        if (idx == -1 || idx == chunk.Count)
        {
            while (++chunkIdx < chunks.Count)
            {
                chunk = chunks[chunkIdx];
                if (chunks[chunkIdx].Count != 0) break;
            }

            idx = 0;
        }

        return (chunk, idx, chunk[idx]);
    }

    protected void InsertState(T newState, List<List<T>> chunks)
    {
        var (prevChunk, prevIndex, prevState) = GetOverlappingStateFrom(newState, chunks);
        var (nextChunk, _, nextState) = GetNextStateFrom(newState, chunks);

        UpdateToPreviousStateOnInsert(newState, prevState);
        UpdateFromPreviousStateAndNextStateOnInsert(newState, prevState, nextState);
        UpdateFromNextStateOnInsert(newState, nextState);
        UpdateToNextStateOnInsert(newState, nextState);

        var (_, chunk) = GetChunk(chunks, newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
            chunk.Insert(prevIndex + 1, newState);
    }

    protected virtual void UpdateToPreviousStateOnInsert(T newState, T prevState) =>
        prevState.EndTime = newState.StartTime;

    protected virtual void UpdateFromNextStateOnInsert(T newState, T nextState) =>
        newState.EndTime = nextState.StartTime;

    protected virtual void UpdateToNextStateOnInsert(T newState, T prevState) { }

    protected virtual void UpdateFromPreviousStateAndNextStateOnInsert(T newState, T prevState, T nextState) { }

    protected virtual void UpdateToNextStateOnInsertConsequent(T newState, T nextState) { }

    protected void UpdateConsequentStateAfterInsertFrom(T currState, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, currState.StartTime);
        var index = chunk.IndexOf(currState) + 1;
        while (chunkIdx < chunks.Count)
        {
            chunk = chunks[chunkIdx];
            for (; index < chunk.Count; index++) UpdateToNextStateOnInsertConsequent(currState, chunk[index]);
            index = 0;
            chunkIdx++;
        }
    }

    protected virtual T UpdateToNextStateOnRemove(T currState, T nextState) => nextState;

    protected void UpdateConsequentStateBeforeRemoveFrom(T currState, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, currState.StartTime);
        var index = chunk.IndexOf(currState) + 1;
        while (chunkIdx < chunks.Count)
        {
            chunk = chunks[chunkIdx];
            for (; index < chunk.Count; index++) chunk[index] = UpdateToNextStateOnRemove(currState, chunk[index]);
            index = 0;
            chunkIdx++;
        }
    }

    protected T RemoveState(T stateToRemove, List<List<T>> chunks)
    {
        var (_, currChunk) = GetChunk(chunks, stateToRemove.StartTime);
        var (_, _, prevState) = GetPreviousStateFrom(stateToRemove, chunks);
        var (_, _, nextState) = GetNextStateFrom(stateToRemove, chunks);

        UpdatePreviousAndNextStateOnRemove(prevState, nextState, stateToRemove);
        currChunk.Remove(stateToRemove);

        return stateToRemove;
    }

    protected T RemoveState(BaseEvent evt, List<List<T>> chunks)
    {
        var (_, _, state) = GetStateFrom(evt, chunks);
        return RemoveState(state, chunks);
    }

    protected virtual void
        UpdatePreviousAndNextStateOnRemove(T prevState, T nextState, T currState) =>
        prevState.EndTime = nextState.StartTime;

    private static int BinarySearch(List<T> chunk, float time)
    {
        var right = chunk.Count - 1;
        var left = 0;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            if (chunk[mid].StartTime <= time && time < chunk[mid].EndTime) return mid;
            if (chunk[mid].StartTime <= time)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }
}

public enum EventPriority
{
    ColorBoost,
    SpecialFX,
    Translation,
    Rotation,
    Light,
}


public class EventStateChunksContainer<T> where T : BasicEventState
{
    public T CurrentState;
    public int CurrentChunkIndex;
    public int CurrentLocalIndex;
    public readonly List<List<T>> Chunks = new();
}

public abstract class BasicEventState : IEquatable<BasicEventState>
{
    private static int ID;
    private readonly int id = ID++; // maybe reference equality is better, idk

    protected BasicEventState(BaseEvent evt) => BaseEvent = evt;

    public BaseEvent BaseEvent;
    public float StartTime = float.MinValue;
    public float EndTime = float.MaxValue;

    public bool Equals(BasicEventState other) => id == other!.id;
}
