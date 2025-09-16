using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEditor.VersionControl;
using UnityEngine;

public abstract class TrackLaneRingsManagerBase : BasicEventManager<RingRotationState>
{
    public RingFilter RingFilter;
    private readonly Dictionary<int, List<RingRotationState>> ringTypeStatesMap = new();
    private readonly Dictionary<int, int> ringTypeIndexMap = new();

    public abstract void HandlePositionEvent(BaseEvent evt);

    public abstract void HandleRotationEvent(BaseEvent evt);

    public abstract Object[] GetToDestroy();

    public override void UpdateTime(float currentTime)
    {
        foreach (var (type, states) in ringTypeStatesMap)
        {
            var currentIndex = ringTypeIndexMap[type];
            var (idx, state) = GetCurrentState(currentTime, currentIndex, states);

            if (currentIndex == idx) continue;
            ringTypeIndexMap[type] = idx;
            UpdateObject(state);
        }
    }

    private void UpdateObject(RingRotationState state)
    {
        var evt = state.BaseEvent;
        switch (evt.Type)
        {
            case 8:
                if (evt.CustomNameFilter != null)
                {
                    var filter = evt.CustomNameFilter;
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        if (RingFilter == RingFilter.Big) HandleRotationEvent(evt);
                    }
                    else if (filter.Contains("Small") || filter.Contains("Panels") || filter.Contains("Triangle"))
                    {
                        if (RingFilter == RingFilter.Small) HandleRotationEvent(evt);
                    }
                    else
                        HandleRotationEvent(evt);
                }
                else
                    HandleRotationEvent(evt);

                break;
            case 9:
                HandlePositionEvent(evt);
                break;
        }
    }

    protected override RingRotationState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var type in new List<int> { 8, 9 }.Where(type => !ringTypeStatesMap.ContainsKey(type)))
        {
            ringTypeIndexMap[type] = 0;
            ringTypeStatesMap[type] = new List<RingRotationState>();
            InitializeStates(ringTypeStatesMap[type]);
            foreach (var state in ringTypeStatesMap[type]) state.BaseEvent.Type = type;
        }

        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, ringTypeStatesMap[evt.Type]);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = ringTypeStatesMap[evt.Type].Find(s => s.BaseEvent == evt);
        RemoveState(state, ringTypeStatesMap[evt.Type]);
    }

    public override void Reset()
    {
        foreach (var (type, states) in ringTypeStatesMap)
        {
            var index = ringTypeIndexMap[type];
            UpdateObject(states[index]);
        }
    }
}

public struct RingRotationState : IBasicEventState
{
    public BaseEvent BaseEvent { get; set; }
    public float StartTime { get; set; }
    public float EndTime { get; set; }
}

public enum RingFilter
{
    Big,
    Small
}
