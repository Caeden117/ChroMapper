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
       

    public override void SetLightEventAppearance(EventAppearanceSO so, BeatmapLightColorEventContainer con, float time, int i)
    {
        so.SetLightColorEventAppearance(con, LightEventsContainer.eventsContainer.AllBoostEvents.FindLast(x => x.Time <= time)?.Value == 1, i);
    }
}
