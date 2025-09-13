using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public class ColorBoostManager
{
    public struct BoostStateData
    {
        public float StartTime;
        public float EndTime;
        public bool State;
    }

    public AudioTimeSyncController atsc;
    public List<BoostStateData> StatesTime = new();
    public (int index, BoostStateData data) Current;
    public bool State;

    public event Action<bool> OnStateChange;

    public void UpdateTime(float currentTime)
    {
        var (idx, data) = Current;
        if (data.EndTime <= currentTime)
        {
            while (++idx < StatesTime.Count)
            {
                data = StatesTime[idx];
                if (data.EndTime > currentTime) break;
            }

            Current = (idx, data);
        }
        else if (data.StartTime > currentTime)
        {
            while (--idx >= 0)
            {
                data = StatesTime[idx];
                if (data.StartTime <= currentTime) break;
            }

            Current = (idx, data);
        }

        if (data.State == State) return;
        State = data.State;
        OnStateChange(State);
    }

    public void BuildBoostEventData(IEnumerable<BaseEvent> events)
    {
        var previousTime = float.MaxValue;
        StatesTime = events
            .Reverse()
            .Select(e =>
            {
                var data = new BoostStateData
                {
                    StartTime = e.SongBpmTime, EndTime = previousTime, State = e.Value == 1
                };
                previousTime = e.SongBpmTime;
                return data;
            })
            .Reverse()
            .ToList();
        if (StatesTime.Count == 0 || StatesTime[0].StartTime != 0f)
            StatesTime.Insert(0, new BoostStateData { EndTime = previousTime });
        Current = (0, StatesTime[0]);

        UpdateTime(atsc.CurrentSongBpmTime);
    }
}
