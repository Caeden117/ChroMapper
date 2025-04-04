using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class GagaDiskManager : MonoBehaviour
{
    private readonly int minEventValue = 0;
    private readonly int maxEventValue = 8;
    private readonly int[] heightEventTypes = new[] { 18, 16, 12, 13, 17, 19 };
    
    public List<GagaDisk> Disks = new List<GagaDisk>();
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
            disk.Init(20); 
            
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
        foreach (var disk in Disks)
        {
            disk.LateUpdateDisk(atsc.CurrentJsonTime);
        }
    }

    public void HandlePositionEvent(BaseEvent evt)
    {
        var value = evt.Value;
        Disks.Where(d => d.HeightEventType == evt.Type)
            .ToList()
            .ForEach(d =>
            {
                var nextEvent = GetNextHeightEvent(evt);
                if (nextEvent != null)
                {
                    var fromValue = evt.Value;
                    var toValue = nextEvent.Value;
                    var toTime = nextEvent.JsonTime;
                    d.SetPosition(ClampValue(fromValue), ClampValue(toValue), evt.JsonTime, toTime);
                }
            });
    }
    
    private List<BaseEvent> GetHeightEventsFromGrid()
    {
        return eventGridContainer.AllUtilityEvents.Where(x => heightEventTypes.Contains(x.Type))
            .Concat(eventGridContainer.AllLaserRotationEvents).Where(x => heightEventTypes.Contains(x.Type))
            .OrderBy(x => x.JsonTime).ToList();
    }

    private int ClampValue(int value) => Math.Clamp(value, minEventValue, maxEventValue);

    private List<BaseEvent> GetCachedHeightEvents(int type) =>
        cachedHeightEvents.TryGetValue(type, out var evts) ? evts : new();
    
    private BaseEvent GetNextHeightEvent(BaseEvent e)
    { ;
        var heightEvents = GetCachedHeightEvents(e.Type);
        
        if (!heightEvents.Any()) return null;
        
        return heightEvents.FirstOrDefault(ev => ev.JsonTime > e.JsonTime);
    }
    private void UpdateEventCache(BaseEvent evt)
    {
        if (!heightEventTypes.Contains(evt.Type)) return; 

        var events = GetHeightEventsFromGrid().Where(x => x.Type == evt.Type);
        
        // Only update the specific type array.
        if (cachedHeightEvents.ContainsKey(evt.Type)) cachedHeightEvents[evt.Type].Clear();
        else cachedHeightEvents[evt.Type] = new List<BaseEvent>();
        
        cachedHeightEvents[evt.Type].AddRange(events);
    }
    
    private void UpdateEventCache(int eventType)
    {
        var events = GetHeightEventsFromGrid().Where(x => x.Type == eventType);

        // Only update the specific type array.
        if (cachedHeightEvents.ContainsKey(eventType)) cachedHeightEvents[eventType].Clear();
            else cachedHeightEvents[eventType] = new List<BaseEvent>();
        
        cachedHeightEvents[eventType].AddRange(events);
    }
}
