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
        float currentTime,
        T currentState,
        List<List<T>> chunks)
    {
        return Atsc.IsPlaying
            ? UseCurrentOrNextState(currentTime, currentState, chunks)
            : UseCurrentOrJumpState(currentTime, currentState, chunks);
    }

    private T UseCurrentOrNextState(
        float currentTime,
        T currentState,
        List<List<T>> chunks) =>
        !(currentState.EndTime <= currentTime) ? currentState : GetStateAt(currentTime, chunks);

    private T UseCurrentOrJumpState(
        float currentTime,
        T currentState,
        List<List<T>> chunks) =>
        currentState.StartTime <= currentTime && currentTime < currentState.EndTime
            ? currentState
            : GetStateAt(currentTime, chunks);

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
        var (prevChunk, _, prevState) = GetOverlappingStateFrom(newState, chunks);
        var (nextChunk, _, nextState) = GetNextStateFrom(newState, chunks);

        UpdateToPreviousStateOnInsert(newState, prevState);
        UpdateFromNextStateOnInsert(newState, nextState);

        var (_, chunk) = GetChunk(chunks, newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
        {
            var idx = chunk
                .FindLastIndex(s =>
                    s.StartTime <= newState.StartTime && newState.StartTime < s.EndTime);
            chunk.Insert(
                idx + 1,
                newState);
        }
    }

    protected virtual void UpdateToPreviousStateOnInsert(T newState, T previousState) =>
        previousState.EndTime = newState.StartTime;

    protected virtual void UpdateFromNextStateOnInsert(T newState, T nextState) =>
        newState.EndTime = nextState.StartTime;

    private void RemoveState(T stateToRemove, List<List<T>> chunks)
    {
        var (_, _, prevState) = GetPreviousStateFrom(stateToRemove, chunks);
        var (_, _, nextState) = GetNextStateFrom(stateToRemove, chunks);
        var (_, currChunk) = GetChunk(chunks, stateToRemove.StartTime);

        UpdatePreviousAndNextStateOnRemove(prevState, nextState, stateToRemove);
        currChunk.Remove(stateToRemove);
    }

    protected void RemoveState(BaseEvent evt, List<List<T>> chunks) => RemoveState(GetStateFrom(evt, chunks), chunks);

    protected virtual void
        UpdatePreviousAndNextStateOnRemove(T previousState, T nextState, T currentState) =>
        previousState.EndTime = nextState.StartTime;
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

    private readonly int id = ID++;
    public BaseEvent BaseEvent;
    public float StartTime;
    public float EndTime;

    public bool Equals(BasicEventState other) => id == other.id;
}
