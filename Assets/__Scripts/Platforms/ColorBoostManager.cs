using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostState>
{
    public List<ColorBoostState> StateTimes = new();
    public int CurrentIndex;
    public bool Boost;

    public event Action<bool> OnStateChange;

    public override void UpdateTime(float currentTime)
    {
        var (idx, state) = Atsc.IsPlaying
            ? UseCurrentOrNextState(currentTime, CurrentIndex, StateTimes)
            : SearchCurrentState(currentTime, CurrentIndex, StateTimes);
        CurrentIndex = idx;
        
        if (state.Boost == Boost) return;
        Boost = state.Boost;
        OnStateChange(Boost);
    }

    public void BuildBoostEventData(IEnumerable<BaseEvent> events)
    {
        var previousTime = float.MaxValue;
        StateTimes = events
            .Reverse()
            .Select(e =>
            {
                var data = new ColorBoostState
                {
                    StartTime = e.SongBpmTime, EndTime = previousTime, Boost = e.Value == 1
                };
                previousTime = e.SongBpmTime;
                return data;
            })
            .Reverse()
            .ToList();
        if (StateTimes.Count == 0 || StateTimes[0].StartTime != 0f)
            StateTimes.Insert(0, new ColorBoostState { EndTime = previousTime });
        CurrentIndex = 0;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    public override void InsertEvent(BaseEvent evt) => throw new NotImplementedException();
    public override void ModifyEvent(BaseEvent oldEvt, BaseEvent newEvt) => throw new NotImplementedException();
    public override void RemoveEvent(BaseEvent evt) => throw new NotImplementedException();
}

public struct ColorBoostState : IBasicEventState
{
    public float StartTime { get; set; }
    public float EndTime { get; set; }
    public bool Boost;
}
