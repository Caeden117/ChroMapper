using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class PauseToggleLights : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [FormerlySerializedAs("events")] [SerializeField] private EventGridContainer eventGrid;

    private readonly BaseEvent defaultBoostEvent = new V2Event(0, 5, 0);
    private readonly List<BaseEvent> lastChromaEvents = new List<BaseEvent>();
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
        var envName = EnvironmentInfoHelper.GetName();
        var isV3 = BeatSaberSongContainer.Instance.Map.GetVersion() == 3;

        lastEvents.Clear();
        lastChromaEvents.Clear();

        if (descriptor == null)
            return;

        if (isPlaying)
        {
            var allEvents = eventGrid.LoadedObjects.Cast<BaseEvent>().Reverse();
            foreach (var e in allEvents)
            {
                if (e.Time <= atsc.CurrentBeat && !e.IsLegacyChroma)
                {
                    if (!lastEvents.ContainsKey(e.Type)) lastEvents.Add(e.Type, new LastEvents());

                    var d = lastEvents[e.Type];
                    if (e.IsLightID && d.LastEvent == null)
                    {
                        foreach (var i in e.CustomLightID.Distinct().Where(x => !d.LastLightIdEvents.ContainsKey(x))
                            .ToArray())
                        {
                            d.LastLightIdEvents.Add(i, e);
                        }
                    }
                    else if (!e.IsLightID && d.LastEvent == null)
                    {
                        d.LastEvent = e;
                    }
                }
                else if (lastEvents.ContainsKey(e.Type) && e.IsLegacyChroma)
                {
                    lastChromaEvents.Add(e);
                }
            }

            // We handle Boost Lights first to set the correct colors
            descriptor.EventPassed(isPlaying, 0,
                lastEvents.ContainsKey((int)EventTypeValue.ColorBoost)
                    ? lastEvents[(int)EventTypeValue.ColorBoost].LastEvent
                    : defaultBoostEvent);

            var blankEvent = new V3BasicEvent(0, 0, 0);
            for (var i = 0; i < 16; i++)
            {
                // Boost light events are already handled above; skip them.
                if (i == (int)EventTypeValue.ColorBoost) continue;

                blankEvent.Type = i;
                if (lastEvents.ContainsKey(i) && lastEvents[i].LastEvent == null) lastEvents[i].LastEvent = blankEvent;

                // No events with this event type exist prior to this time; pass a blank event and skip.
                if (!lastEvents.ContainsKey(i))
                {
                    if (blankEvent.IsRingEvent() || blankEvent.IsLaneRotationEvent()) continue;
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
                    (!regular.IsLightEvent(envName) || (regular.Value != (int)LightValue.BlueFade &&
                                                        regular.Value != (int)LightValue.RedFade)) &&
                    // ... and it's not a ring event
                    !regular.IsRingEvent(envName))
                {
                    descriptor.EventPassed(isPlaying, 0, regular);
                }
                // Pass an empty even if it is not a ring or rotation event, OR it is null.
                else if (regular is null || (!regular.IsRingEvent(envName) && !regular.IsLaneRotationEvent()))
                {
                    descriptor.EventPassed(isPlaying, 0, isV3 ? (BaseEvent)new V3BasicEvent(0, i, 0) : new V2Event(0, i, 0));
                    continue;
                }

                // Chroma light prop
                foreach (var propEvent in regularEvents.LastPropEvents)
                    descriptor.EventPassed(isPlaying, 0, propEvent.Value);

                foreach (var propEvent in regularEvents.LastLightIdEvents)
                    descriptor.EventPassed(isPlaying, 0, propEvent.Value);

                if (regular.IsLightEvent(envName) && Settings.Instance.EmulateChromaLite)
                    descriptor.EventPassed(isPlaying, 0, chroma ?? (isV3
                        ? (BaseEvent)new V3BasicEvent(0, i, ColourManager.RGBReset)
                        : new V2Event(0, i, ColourManager.RGBReset)));
            }
        }
        else
        {
            var leftSpeedReset =
                isV3
                    ? (BaseEvent)new V3BasicEvent(0, (int)EventTypeValue.LeftLaserRotation, 0)
                    {
                        CustomData = new JSONObject()
                    }
                    : new V2Event(0, (int)EventTypeValue.LeftLaserRotation, 0)
            {
                CustomData = new JSONObject()
            };
            leftSpeedReset.CustomLockRotation = true;
            var rightSpeedReset = isV3 ? (BaseEvent)new V3BasicEvent(0, (int)EventTypeValue.RightLaserRotation, 0)
            {
                CustomData = new JSONObject()
            } : new V2Event(0, (int)EventTypeValue.RightLaserRotation, 0)
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
        public BaseEvent LastEvent;
        public readonly Dictionary<int, BaseEvent> LastLightIdEvents = new Dictionary<int, BaseEvent>();
        public readonly Dictionary<int, BaseEvent> LastPropEvents = new Dictionary<int, BaseEvent>();
    }
}
