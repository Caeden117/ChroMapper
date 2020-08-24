using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    private HashSet<int> eventTypesHash = new HashSet<int>();
    private List<MapEvent> lastEvents = new List<MapEvent>();
    private List<MapEvent> lastChromaEvents = new List<MapEvent>();

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
        eventTypesHash.Clear();
        lastEvents.Clear();
        lastChromaEvents.Clear();
        if (isPlaying)
        {
            IEnumerable<MapEvent> allEvents = events.LoadedObjects.Cast<MapEvent>().Reverse();
            foreach (MapEvent e in allEvents)
            {
                if (e._time <= atsc.CurrentBeat && eventTypesHash.Add(e._type) && !e.IsLegacyChromaEvent)
                {
                    lastEvents.Add(e);
                }
                else if (eventTypesHash.Contains(e._type) && e.IsLegacyChromaEvent)
                {
                    lastChromaEvents.Add(e);
                }
            }
            // We handle Boost Lights first to set the correct colors
            if (eventTypesHash.Contains(MapEvent.EVENT_TYPE_BOOST_LIGHTS))
            {
                descriptor.EventPassed(false, 0, lastEvents.First(x => x._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS));
            }
            MapEvent blankEvent = new MapEvent(0, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                // Boost light events are already handled above; skip them.
                if (i == MapEvent.EVENT_TYPE_BOOST_LIGHTS) continue;
                // No events with this event type exist prior to this time; pass a blank event and skip.
                if (!eventTypesHash.Contains(i))
                {
                    blankEvent._type = i;
                    if (blankEvent.IsRingEvent || blankEvent.IsRotationEvent) continue;
                    descriptor.EventPassed(false, 0, blankEvent);
                    continue;
                }

                // Grab all the events of the type, and that are behind current beat
                MapEvent regular = lastEvents.FirstOrDefault(x => x._type == i);
                MapEvent chroma = lastChromaEvents.FirstOrDefault(x => x._type == i);

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

                if (!regular.IsUtilityEvent)
                {
                    if (chroma != null)
                        descriptor.EventPassed(false, 0, chroma);
                    else descriptor.EventPassed(false, 0, new MapEvent(0, i, ColourManager.RGB_RESET));
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
            descriptor.KillLights();
        }
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        atsc.OnPlayToggle -= PlayToggle;
    }
}
