using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class PauseToggleLights : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventGridContainer eventGrid;

    private readonly BaseEvent defaultBoostEvent = new BaseEvent { Type = (int)EventTypeValue.ColorBoost };

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

    // TODO(Caeden): oh go dwhat the fuck is this, optimize it???
    // it's me, john optimise
    private void PlayToggle(bool isPlaying)
    {
        lastEvents.Clear();

        if (descriptor == null) return;

        if (isPlaying)
        {
            var allEvents = eventGrid.MapObjects.Reverse<BaseEvent>();
            foreach (var e in allEvents)
            {
                if (e.JsonTime <= atsc.CurrentJsonTime && !e.IsLegacyChroma)
                {
                    if (!lastEvents.ContainsKey(e.Type)) lastEvents.Add(e.Type, new LastEvents());

                    var d = lastEvents[e.Type];
                    if (e.CustomLightID != null && d.LastEvent == null)
                    {
                        foreach (var i in e
                            .CustomLightID.Distinct()
                            .Where(x => !d.LastLightIdEvents.ContainsKey(x))
                            .ToArray())
                            d.LastLightIdEvents.Add(i, e);
                    }
                    else if (e.CustomLightID == null && d.LastEvent == null) d.LastEvent = e;
                }
            }

            var blankEvent = new BaseEvent();
            for (var i = 0; i < 16; i++)
            {
                // Boost light events are already handled above; skip them.
                if (i == (int)EventTypeValue.ColorBoost) continue;

                blankEvent.Type = i;
                if (lastEvents.ContainsKey(i) && lastEvents[i].LastEvent == null) lastEvents[i].LastEvent = blankEvent;

                var gagaActive = descriptor.DiskManager != null
                    && (blankEvent.IsLaserRotationEvent() || blankEvent.IsUtilityEvent());
                if (gagaActive) continue;

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

                // Past the last event if we have an event to pass in the first place
                if (regular != null
                    &&
                    // ... it's not a fade event
                    (!regular.IsLightEvent()
                        || (regular.Value != (int)LightValue.BlueFade && regular.Value != (int)LightValue.RedFade))
                    &&
                    // ... and it's not a ring event
                    !regular.IsRingEvent())
                    descriptor.EventPassed(isPlaying, 0, regular);
                // Pass an empty even if it is not a ring or rotation event, OR it is null.
                else if (regular is null || (!regular.IsRingEvent() && !regular.IsLaneRotationEvent()))
                    descriptor.EventPassed(isPlaying, 0, new BaseEvent { Type = i });
            }
        }
        else
        {
            if (descriptor.DiskManager == null)
            {
                var leftSpeedReset = new BaseEvent
                {
                    Type = (int)EventTypeValue.LeftLaserRotation, CustomLockRotation = true
                };
                var rightSpeedReset = new BaseEvent
                {
                    Type = (int)EventTypeValue.RightLaserRotation, CustomLockRotation = true
                };
                descriptor.EventPassed(isPlaying, 0, leftSpeedReset);
                descriptor.EventPassed(isPlaying, 0, rightSpeedReset);
            }
        }
    }

    private class LastEvents
    {
        public BaseEvent LastEvent;
        public readonly Dictionary<int, BaseEvent> LastLightIdEvents = new Dictionary<int, BaseEvent>();
        public readonly Dictionary<int, BaseEvent> LastPropEvents = new Dictionary<int, BaseEvent>();
    }
}
