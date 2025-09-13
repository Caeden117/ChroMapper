using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class BasicEventManager<T> : MonoBehaviour where T : IBasicEventState
{
    public AudioTimeSyncController Atsc;

    public abstract void UpdateTime(float time);

    public virtual (int index, T data) UseCurrentOrNextState(
        float currentTime,
        int currentIndex,
        List<T> datas)
    {
        var currentData = datas[currentIndex];
        if (!(currentData.EndTime <= currentTime)) return (currentIndex, currentData);
        while (++currentIndex < datas.Count)
        {
            currentData = datas[currentIndex];
            if (currentData.EndTime > currentTime) break;
        }

        return (currentIndex, currentData);
    }

    public virtual (int index, T currentData) SearchCurrentState(
        float currentTime,
        int currentIndex,
        List<T> datas)
    {
        var half = Mathf.CeilToInt(datas.Count / 2f);
        while (true) // scary
        {
            var currentData = datas[currentIndex];
            if (currentData.EndTime <= currentTime)
            {
                int nextIndex;
                do
                {
                    half = Mathf.CeilToInt(half / 2f);
                    nextIndex = currentIndex + half;
                } while (nextIndex >= datas.Count);

                currentIndex = nextIndex;
            }
            else if (currentData.StartTime > currentTime)
            {
                int nextIndex;
                do
                {
                    half = Mathf.CeilToInt(half / 2f);
                    nextIndex = currentIndex - half;
                } while (nextIndex < 0);

                currentIndex = nextIndex;
            }
            else
                return (currentIndex, currentData);
        }
    }

    public abstract void InsertEvent(BaseEvent evt);
    public abstract void ModifyEvent(BaseEvent oldEvt, BaseEvent newEvt); // not sure if possible
    public abstract void RemoveEvent(BaseEvent evt);
}

public interface IBasicEventState
{
    float StartTime { get; set; }
    float EndTime { get; set; }
}
