using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class BasicEventManager : MonoBehaviour
{
    public EventPriority Priority;
    public AudioTimeSyncController Atsc;

    public abstract void UpdateTime(float time);
    public abstract void BuildFromEvents(IEnumerable<BaseEvent> events);
    public abstract void InsertEvent(BaseEvent evt);
    public abstract void RemoveEvent(BaseEvent evt);
    public abstract void Reset();
}

public abstract class BasicEventManager<T> : BasicEventManager where T : BasicEventState
{
    private const float chunkByTime = 10f;

    private (int index, List<T> chunk) GetChunk(List<List<T>> chunks, float v)
    {
        var index = Mathf.Clamp(Mathf.FloorToInt(v / chunkByTime), 0, chunks.Count - 1);
        return (index, chunks[index]);
    }

    protected abstract T CreateState(BaseEvent evt);

    protected void InitializeStates(List<List<T>> chunks)
    {
        chunks.Clear();
        for (var time = 0f; time < Atsc.SongAudioSource.clip.length; time += Atsc.GetSecondsFromBeat(chunkByTime))
            chunks.Add(new List<T>());
        var start = CreateState(new BaseEvent());
        var end = CreateState(new BaseEvent());
        end.StartTime = start.EndTime;
        chunks[0].Add(start);
        chunks[^1].Add(end);
    }

    protected T GetCurrentState(
        float currTime,
        T currState,
        List<List<T>> chunks)
    {
        return Atsc.IsPlaying
            ? UseCurrentOrNextState(currTime, currState, chunks)
            : UseCurrentOrFindState(currTime, currState, chunks);
    }

    private T UseCurrentOrNextState(
        float currTime,
        T currState,
        List<List<T>> chunks) =>
        currTime < currState.EndTime ? currState : GetStateAt(currTime, chunks);

    private T UseCurrentOrFindState(
        float currTime,
        T currState,
        List<List<T>> chunks) =>
        currState.StartTime <= currTime && currTime < currState.EndTime
            ? currState
            : GetStateAt(currTime, chunks);

    protected T GetStateAt(float time, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, time);
        var idx = chunk
            .FindLastIndex(s =>
                s.StartTime <= time && time < s.EndTime);

        if (idx == -1)
        {
            while (--chunkIdx >= 0)
            {
                chunk = chunks[chunkIdx];
                if (chunks[chunkIdx].Count != 0) break;
            }

            idx = chunk.Count - 1;
        }

        return chunk[idx];
    }

    protected T GetStateFrom(
        BaseEvent evt,
        List<List<T>> chunks) =>
        GetStateAt(evt.SongBpmTime, chunks);

    protected int GetStateIndex(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = chunk.IndexOf(state);
        for (var i = 0; i < chunkIdx; i++) idx += chunks[i].Count;
        return idx;
    }

    private (List<T> chunk, int index, T state) GetPreviousStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = chunk
                .FindLastIndex(s =>
                    s.StartTime <= state.StartTime && state.StartTime < s.EndTime)
            - 1;

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

    private (List<T> chunk, int index, T state) GetOverlappingStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = chunk
            .FindLastIndex(s =>
                s.StartTime <= state.StartTime && state.StartTime < s.EndTime);

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

    private (List<T> chunk, int index, T state) GetNextStateFrom(T state, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, state.StartTime);
        var idx = chunk
                .FindLastIndex(s =>
                    s.StartTime <= state.StartTime && state.StartTime < s.EndTime)
            + 1;

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
        UpdateFromNextStateOnInsert(newState, nextState);

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

    protected virtual T UpdateToNextStateOnInsert(T newState, T nextState) => nextState;

    protected void UpdateConsequentStateAfterInsertFrom(T currState, List<List<T>> chunks)
    {
        var (chunkIdx, chunk) = GetChunk(chunks, currState.StartTime);
        var index = chunk.IndexOf(currState) + 1;
        while (chunkIdx < chunks.Count)
        {
            chunk = chunks[chunkIdx];
            for (; index < chunk.Count; index++) chunk[index] = UpdateToNextStateOnInsert(currState, chunk[index]);
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

    protected T RemoveState(BaseEvent evt, List<List<T>> chunks) => RemoveState(GetStateFrom(evt, chunks), chunks);

    protected virtual void
        UpdatePreviousAndNextStateOnRemove(T prevState, T nextState, T currState) =>
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


public class EventStateChunksContainer<T> where T : BasicEventState
{
    public T Current;
    public readonly List<List<T>> Chunks = new();
}

public abstract class BasicEventState : IEquatable<BasicEventState>
{
    private static int ID;
    private readonly int id = ID++; // maybe reference equality is better, idk

    public BaseEvent BaseEvent;
    public float StartTime;
    public float EndTime;

    public bool Equals(BasicEventState other) => id == other!.id;
}
