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

public abstract class BasicEventManager<T> : BasicEventManager where T : IBasicEventState
{
    protected abstract T CreateState(BaseEvent evt);

    protected void InitializeStates(List<T> states)
    {
        states.Clear();
        var start = CreateState(new BaseEvent());
        var end = CreateState(new BaseEvent());
        end.StartTime = start.EndTime;
        states.Add(start);
        states.Add(end);
    }

    protected (int index, T state) GetCurrentState(
        float currentTime,
        int currentIndex,
        List<T> states)
    {
        return Atsc.IsPlaying
            ? UseCurrentOrNextState(currentTime, currentIndex, states)
            : SearchCurrentState(currentTime, currentIndex, states);
    }

    protected virtual (int index, T state) UseCurrentOrNextState(
        float currentTime,
        int currentIndex,
        List<T> states)
    {
        var currentState = states[currentIndex];
        if (!(currentState.EndTime <= currentTime)) return (currentIndex, currentState);
        while (++currentIndex < states.Count)
        {
            currentState = states[currentIndex];
            if (currentState.EndTime > currentTime) break;
        }

        return (currentIndex, currentState);
    }

    protected virtual (int index, T currentState) SearchCurrentState(
        float currentTime,
        int currentIndex,
        List<T> states)
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, states.Count - 1);
        var half = states.Count / 2;
        while (true) // scary
        {
            var currentState = states[currentIndex];
            if (currentState.EndTime <= currentTime)
            {
                int nextIndex;
                do
                {
                    half = (half + 1) / 2;
                    nextIndex = currentIndex + half;
                } while (nextIndex >= states.Count);

                currentIndex = nextIndex;
            }
            else if (currentState.StartTime > currentTime)
            {
                int nextIndex;
                do
                {
                    half = (half + 1) / 2;
                    nextIndex = currentIndex - half;
                } while (nextIndex < 0);

                currentIndex = nextIndex;
            }
            else
                return (currentIndex, currentState);
        }
    }

    protected virtual void InsertState(T newState, List<T> states)
    {
        int currentIndex;
        if (states[^2].StartTime < newState.StartTime)
            currentIndex = states.Count - 2;
        else
        {
            currentIndex = states.FindLastIndex(state =>
                state.StartTime <= newState.StartTime && newState.StartTime < state.EndTime);
        }

        var previousState = states[currentIndex];
        var nextState = states[currentIndex + 1];

        UpdateToPreviousStateOnInsert(ref newState, ref previousState);
        UpdateFromNextStateOnInsert(ref newState, ref nextState);

        states[currentIndex] = previousState;
        states.Insert(currentIndex + 1, newState);
    }

    protected virtual void UpdateToPreviousStateOnInsert(ref T newState, ref T previousState) =>
        previousState.EndTime = newState.StartTime;

    protected virtual void UpdateFromNextStateOnInsert(ref T newState, ref T nextState) =>
        newState.EndTime = nextState.StartTime;

    protected virtual void RemoveState(T currentState, List<T> states)
    {
        var index = states.IndexOf(currentState);
        var previousState = states[index - 1];
        var nextState = states[index + 1];

        UpdatePreviousAndNextStateOnRemove(ref previousState, ref nextState, ref currentState);
        states[index - 1] = previousState;
        states[index + 1] = nextState;
        states.RemoveAt(index);
    }

    protected virtual void
        UpdatePreviousAndNextStateOnRemove(ref T previousState, ref T nextState, ref T currentState) =>
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

public interface IBasicEventState
{
    BaseEvent BaseEvent { get; set; }
    float StartTime { get; set; }
    float EndTime { get; set; }
}
