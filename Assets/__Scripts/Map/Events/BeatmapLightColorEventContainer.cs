using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// It's strange to inherit from <see cref="BeatmapEventContainer"/>, but I need to use those shader properties to reuse <see cref="EventAppearanceSO"/>
/// </summary>
public class BeatmapLightColorEventContainer : BeatmapLightEventContainerBase<
    BeatmapLightColorEvent, 
    BeatmapLightColorEventBox, 
    BeatmapLightColorEventData, 
    BeatmapLightColorEventContainer, 
    LightColorEventsContainer,
    LightingEvent
    >
{
    private readonly int[] colorInvertMap = { 1, 0, 1 };

    protected override void InvertEventImpl(ref BeatmapLightColorEventData ebd)
    {
        ebd.Color = colorInvertMap.ElementAtOrDefault(ebd.Color);
    }

    protected override void TweakValueImpl(ref BeatmapLightColorEventData ebd, int modifier)
    {
        ebd.Brightness += modifier * 0.1f;
        ebd.Brightness = Mathf.Clamp(ebd.Brightness, 0, 1.5f);
    }

    public override void SetLightEventAppearance(EventAppearanceSO so, BeatmapLightColorEventContainer con, float time, int i)
    {
        so.SetLightColorEventAppearance(con, LightEventsContainer.eventsContainer.AllBoostEvents.FindLast(x => x.Time <= time)?.Value == 1, i);
    }
}
