using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    private readonly MapEvent defaultBoostEvent = new MapEvent(0, 5, 0);
    private readonly List<MapEvent> lastChromaEvents = new List<MapEvent>();
    private readonly Dictionary<int, LastEvents> lastEvents = new Dictionary<int, LastEvents>();
    private PlatformDescriptor descriptor;

    private void Awake()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
        atsc.PlayToggle += PlayToggle;
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        atsc.PlayToggle -= PlayToggle;
    }

    private void PlatformLoaded(PlatformDescriptor platform) => descriptor = platform;

    private void PlayToggle(bool isPlaying)
    {
        lastEvents.Clear();
        lastChromaEvents.Clear();

        if (descriptor == null)
            return;

        if (isPlaying)
        {
            var allEvents = events.LoadedObjects.Cast<MapEvent>().Reverse();
            foreach (var e in allEvents)
            {
                if (e.Time <= atsc.CurrentBeat && !e.IsLegacyChromaEvent)
                {
                    if (!lastEvents.ContainsKey(e.Type)) lastEvents.Add(e.Type, new LastEvents());

                    var d = lastEvents[e.Type];
                    if (e.IsLightIdEvent && d.LastEvent == null)
                    {
                        foreach (var i in e.LightId.Distinct().Where(x => !d.LastLightIdEvents.ContainsKey(x))
                            .ToArray())
                        {
                            d.LastLightIdEvents.Add(i, e);
                        }
                    }
                    else if (!e.IsLightIdEvent && d.LastEvent == null)
                    {
                        d.LastEvent = e;
                    }
                }
                else if (lastEvents.ContainsKey(e.Type) && e.IsLegacyChromaEvent)
                {
                    lastChromaEvents.Add(e);
                }
            }

            // We handle Boost Lights first to set the correct colors
            descriptor.EventPassed(isPlaying, 0,
                lastEvents.ContainsKey(MapEvent.EventTypeBoostLights)
                    ? lastEvents[MapEvent.EventTypeBoostLights].LastEvent
                    : defaultBoostEvent);

            var blankEvent = new MapEvent(0, 0, 0);
            for (var i = 0; i < 16; i++)
            {
                // Boost light events are already handled above; skip them.
                if (i == MapEvent.EventTypeBoostLights) continue;

                blankEvent.Type = i;
                if (lastEvents.ContainsKey(i) && lastEvents[i].LastEvent == null) lastEvents[i].LastEvent = blankEvent;

                // No events with this event type exist prior to this time; pass a blank event and skip.
                if (!lastEvents.ContainsKey(i))
                {
                    if (blankEvent.IsRingEvent || blankEvent.IsRotationEvent) continue;
                    descriptor.EventPassed(isPlaying, 0, blankEvent);
                    continue;
                }

                // Grab all the events of the type, and that are behind current beat
                var regularEvents = lastEvents[i];
                var regular = regularEvents.LastEvent;
                var chroma = lastChromaEvents.Find(x => x.Type == i);

                // Past the last event if we have an event to pass in the first place
                if (regular != null &&
                    // ... it's not a fade event
                    (regular.IsUtilityEvent || (regular.Value != MapEvent.LightValueBlueFade &&
                                                regular.Value != MapEvent.LightValueRedFade)) &&
                    // ... and it's not a ring event
                    !regular.IsRingEvent)
                {
                    descriptor.EventPassed(isPlaying, 0, regular);
                }
                // Pass an empty even if it is not a ring or rotation event, OR it is null.
                else if (regular is null || (!regular.IsRingEvent && !regular.IsRotationEvent))
                {
                    descriptor.EventPassed(isPlaying, 0, new MapEvent(0, i, 0));
                    continue;
                }

                // Chroma light prop
                foreach (var propEvent in regularEvents.LastPropEvents)
                    descriptor.EventPassed(isPlaying, 0, propEvent.Value);

                foreach (var propEvent in regularEvents.LastLightIdEvents)
                    descriptor.EventPassed(isPlaying, 0, propEvent.Value);

                if (!regular.IsUtilityEvent && Settings.Instance.EmulateChromaLite)
                    descriptor.EventPassed(isPlaying, 0, chroma ?? new MapEvent(0, i, ColourManager.RGBReset));
            }
        }
        else
        {
            var leftSpeedReset = new MapEvent(0, MapEvent.EventTypeLeftLasersSpeed, 0)
            {
                CustomData = new JSONObject()
            };
            leftSpeedReset.CustomLockRotation = true;
            var rightSpeedReset = new MapEvent(0, MapEvent.EventTypeRightLasersSpeed, 0)
            {
                CustomData = new JSONObject()
            };
            rightSpeedReset.CustomLockRotation = true;
            descriptor.EventPassed(isPlaying, 0, leftSpeedReset);
            descriptor.EventPassed(isPlaying, 0, rightSpeedReset);
            descriptor.KillChromaLights();
            descriptor.KillLights();
        }
    }

    private class LastEvents
    {
        public MapEvent LastEvent;
        public readonly Dictionary<int, MapEvent> LastLightIdEvents = new Dictionary<int, MapEvent>();
        public readonly Dictionary<int, MapEvent> LastPropEvents = new Dictionary<int, MapEvent>();
    }
}
