using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    private MapEvent defaultBoostEvent = new MapEvent(0, 5, 0);

    private const int NOT_PROP = -1;
    private Dictionary<int, LastEvents> lastEvents = new Dictionary<int, LastEvents>();
    private List<MapEvent> lastChromaEvents = new List<MapEvent>();
    
    private class LastEvents
    {
        public MapEvent lastEvent = null;
        public Dictionary<int, MapEvent> LastPropEvents = new Dictionary<int, MapEvent>();
        public Dictionary<int, MapEvent> LastLightIdEvents = new Dictionary<int, MapEvent>();
    }

    void Awake()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
        atsc.OnPlayToggle += PlayToggle;
    }

    private void PlatformLoaded(PlatformDescriptor platform)
    {
        descriptor = platform;
    }

    private void PlayToggle(bool isPlaying)
    {
        lastEvents.Clear();
        lastChromaEvents.Clear();

        if (descriptor == null)
            return;

        if (isPlaying)
        {
            IEnumerable<MapEvent> allEvents = events.LoadedObjects.Cast<MapEvent>().Reverse();
            foreach (MapEvent e in allEvents)
            {
                if (e._time <= atsc.CurrentBeat && !e.IsLegacyChromaEvent)
                {
                    if (!lastEvents.ContainsKey(e._type))
                    {
                        lastEvents.Add(e._type, new LastEvents());
                    }

                    var d = lastEvents[e._type];
                    if (e.IsLightIdEvent && d.lastEvent == null)
                    {
                        foreach (var i in e.LightId.Distinct().Where(x => !d.LastLightIdEvents.ContainsKey(x)).ToArray())
                        {
                            d.LastLightIdEvents.Add(i, e);
                        }
                    }
                    else if (!e.IsLightIdEvent && d.lastEvent == null)
                    {
                        d.lastEvent = e;
                    }
                }
                else if (lastEvents.ContainsKey(e._type) && e.IsLegacyChromaEvent)
                {
                    lastChromaEvents.Add(e);
                }
            }

            // We handle Boost Lights first to set the correct colors
            descriptor.EventPassed(false, 0,
                lastEvents.ContainsKey(MapEvent.EVENT_TYPE_BOOST_LIGHTS)
                    ? lastEvents[MapEvent.EVENT_TYPE_BOOST_LIGHTS].lastEvent
                    : defaultBoostEvent);

            MapEvent blankEvent = new MapEvent(0, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                // Boost light events are already handled above; skip them.
                if (i == MapEvent.EVENT_TYPE_BOOST_LIGHTS) continue;

                blankEvent._type = i;
                if (lastEvents.ContainsKey(i) && lastEvents[i].lastEvent == null)
                {
                    lastEvents[i].lastEvent = blankEvent;
                }

                // No events with this event type exist prior to this time; pass a blank event and skip.
                if (!lastEvents.ContainsKey(i))
                {
                    if (blankEvent.IsRingEvent || blankEvent.IsRotationEvent) continue;
                    descriptor.EventPassed(false, 0, blankEvent);
                    continue;
                }

                // Grab all the events of the type, and that are behind current beat
                var regularEvents = lastEvents[i];
                var regular = regularEvents.lastEvent;
                var chroma = lastChromaEvents.Find(x => x._type == i);

                // Past the last event if we have an event to pass in the first place
                if (regular != null &&
                    // ... it's not a fade event
                    (regular.IsUtilityEvent || regular._value != MapEvent.LIGHT_VALUE_BLUE_FADE && regular._value != MapEvent.LIGHT_VALUE_RED_FADE) &&
                    // ... and it's not a ring event
                    !regular.IsRingEvent)
                {
                    descriptor.EventPassed(false, 0, regular);
                }
                // Pass an empty even if it is not a ring or rotation event, OR it is null.
                else if (regular is null || (!regular.IsRingEvent && !regular.IsRotationEvent))
                {
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0));
                    continue;
                }

                // Chroma light prop
                foreach (var propEvent in regularEvents.LastPropEvents)
                {
                    descriptor.EventPassed(false, 0, propEvent.Value);
                }
                
                foreach (var propEvent in regularEvents.LastLightIdEvents)
                {
                    descriptor.EventPassed(false, 0, propEvent.Value);
                }

                if (!regular.IsUtilityEvent && Settings.Instance.EmulateChromaLite)
                {
                    descriptor.EventPassed(false, 0, chroma ?? new MapEvent(0, i, ColourManager.RGB_RESET));
                }
            }
        }
        else
        {
            MapEvent leftSpeedReset = new MapEvent(0, MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED, 0);
            leftSpeedReset._customData = new JSONObject();
            leftSpeedReset._customData["_lockPosition"] = true;
            MapEvent rightSpeedReset = new MapEvent(0, MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED, 0);
            rightSpeedReset._customData = new JSONObject();
            rightSpeedReset._customData["_lockPosition"] = true;
            descriptor.EventPassed(false, 0, leftSpeedReset);
            descriptor.EventPassed(false, 0, rightSpeedReset);
            descriptor.KillChromaLights();
            descriptor.KillLights();
        }
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        atsc.OnPlayToggle -= PlayToggle;
    }
}
