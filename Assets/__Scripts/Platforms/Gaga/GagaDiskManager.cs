using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class GagaDiskManager : BasicEventManager<GagaDiskState>
{
    private const int minEventValue = 0;
    private const int maxEventValue = 8;
    private readonly int[] heightEventTypes = { 18, 16, 12, 13, 17, 19 };

    public List<GagaDisk> Disks = new();
    private EventGridContainer eventGridContainer;
    private AudioTimeSyncController atsc;

    private Dictionary<int, List<BaseEvent>> cachedHeightEvents = new Dictionary<int, List<BaseEvent>>();

    public void Start()
    {
        atsc = FindObjectOfType<AudioTimeSyncController>();
        eventGridContainer = FindObjectOfType<EventGridContainer>();
        foreach (var disk in Disks)
        {
            // Start at Y 20 (default).
            disk.Init();

            // Init cache for each height event type.
            UpdateEventCache(disk.HeightEventType);
        }

        eventGridContainer.ObjectSpawnedEvent += UpdateEventCache;
        eventGridContainer.ObjectDeletedEvent += UpdateEventCache;
    }

    public void OnDestroy()
    {
        eventGridContainer.ObjectSpawnedEvent -= UpdateEventCache;
        eventGridContainer.ObjectDeletedEvent -= UpdateEventCache;
    }

    private void LateUpdate()
    {
        foreach (var disk in Disks) disk.LateUpdateDisk(atsc.CurrentJsonTime);
    }

    public void HandlePositionEvent(BaseEvent evt)
    {
        foreach (var d in Disks.Where(d => d.HeightEventType == evt.Type))
        {
            var nextEvent = GetNextHeightEvent(evt);
            if (nextEvent == null) return;

            var fromValue = evt.Value;
            var toValue = nextEvent.Value;
            var toTime = nextEvent.JsonTime;
            d.SetPosition(
                ClampEventValue(fromValue),
                ClampEventValue(toValue),
                evt.JsonTime,
                toTime);
        }
    }

    private int ClampEventValue(int value) => Math.Clamp(value, minEventValue, maxEventValue);

    private List<BaseEvent> GetHeightEventsFromGrid()
    {
        return eventGridContainer
            .AllUtilityEvents.Where(x => heightEventTypes.Contains(x.Type))
            .Concat(eventGridContainer.AllLaserRotationEvents)
            .Where(x => heightEventTypes.Contains(x.Type))
            .OrderBy(x => x.JsonTime)
            .ToList();
    }

    private List<BaseEvent> GetCachedHeightEvents(int type) =>
        cachedHeightEvents.TryGetValue(type, out var evts) ? evts : new();

    private BaseEvent GetNextHeightEvent(BaseEvent e)
    {
        var heightEvents = GetCachedHeightEvents(e.Type);

        return !heightEvents.Any() ? null : heightEvents.FirstOrDefault(ev => ev.JsonTime > e.JsonTime);
    }

    private BaseEvent GetNextHeightEvent(int type)
    {
        var heightEvents = GetCachedHeightEvents(type);

        return !heightEvents.Any() ? null : heightEvents.FirstOrDefault(ev => ev.JsonTime >= atsc.CurrentJsonTime);
    }

    private BaseEvent GetPreviousHeightEvent(int type)
    {
        var heightEvents = GetCachedHeightEvents(type)
            .Reverse<BaseEvent>()
            .ToList();

        if (!heightEvents.Any()) return null;

        return heightEvents.FirstOrDefault(ev => ev.JsonTime >= atsc.CurrentJsonTime);
    }

    private void UpdateEventCache(BaseEvent evt)
    {
        if (!heightEventTypes.Contains(evt.Type)) return;

        var events = GetHeightEventsFromGrid().Where(x => x.Type == evt.Type);

        // Only update the specific type array.
        if (cachedHeightEvents.ContainsKey(evt.Type))
            cachedHeightEvents[evt.Type].Clear();
        else
            cachedHeightEvents[evt.Type] = new List<BaseEvent>();

        cachedHeightEvents[evt.Type].AddRange(events);

        // Update position queue
        foreach (var disk in Disks)
        {
            if (disk.HeightEventType != evt.Type) continue;
            var prevEvt = GetPreviousHeightEvent(evt.Type);
            var nextEvt = GetNextHeightEvent(evt);

            var fromValue = 4;
            var toValue = 4;
            var fromTime = 0f;
            var toTime = 0.1f;

            if (prevEvt != null)
            {
                fromValue = prevEvt.Value;
                fromTime = prevEvt.JsonTime;
                if (nextEvt != null)
                {
                    toValue = nextEvt.Value;
                    toTime = nextEvt.JsonTime;
                }
            }

            disk.SetPosition(
                ClampEventValue(fromValue),
                ClampEventValue(toValue),
                fromTime,
                toTime
            );
            return;
        }
    }

    private void UpdateEventCache(int eventType)
    {
        var events = GetHeightEventsFromGrid().Where(x => x.Type == eventType);

        // Only update the specific type array.
        if (cachedHeightEvents.ContainsKey(eventType))
            cachedHeightEvents[eventType].Clear();
        else
            cachedHeightEvents[eventType] = new List<BaseEvent>();

        cachedHeightEvents[eventType].AddRange(events);
    }

    private readonly Dictionary<int, List<List<GagaDiskState>>> gagaDiskStateChunksMap = new();
    private readonly Dictionary<int, GagaDiskState> gagaDiskCurrentMap = new();

    public override void UpdateTime(float currentTime)
    {
        foreach (var (type, states) in gagaDiskStateChunksMap)
        {
            var currentState = gagaDiskCurrentMap[type];
            var state = GetCurrentState(currentTime, currentState, states);

            if (currentState == state) continue;
            gagaDiskCurrentMap[type] = state;
            UpdateObject(state.BaseEvent);
        }
    }

    private void UpdateObject(BaseEvent evt) => HandlePositionEvent(evt);

    protected override GagaDiskState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var type in new List<int>
            {
                12,
                13,
                16,
                17,
                18,
                19
            }.Where(type => !gagaDiskStateChunksMap.ContainsKey(type)))
        {
            gagaDiskStateChunksMap[type] = new List<List<GagaDiskState>>();
            InitializeStates(gagaDiskStateChunksMap[type]);
            foreach (var state in gagaDiskStateChunksMap[type].SelectMany(state => state)) state.BaseEvent.Type = type;
            gagaDiskCurrentMap[type] = GetStateAt(0, gagaDiskStateChunksMap[type]);
        }

        foreach (var evt in events) InsertEvent(evt);
    }

    public override void InsertEvent(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        InsertState(state, gagaDiskStateChunksMap[evt.Type]);
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        var state = RemoveState(evt, gagaDiskStateChunksMap[evt.Type]);
        if (gagaDiskCurrentMap[evt.Type] == state)
            gagaDiskCurrentMap[evt.Type] = GetStateAt(evt.SongBpmTime, gagaDiskStateChunksMap[evt.Type]);
    }

    public override void Reset()
    {
        foreach (var state in gagaDiskCurrentMap.Values) UpdateObject(state.BaseEvent);
    }
}

public class GagaDiskState : BasicEventState
{
}
