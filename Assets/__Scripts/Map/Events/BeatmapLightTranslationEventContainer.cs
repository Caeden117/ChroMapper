using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapLightTranslationEventContainer : BeatmapLightEventContainerBase<
    BeatmapLightTranslationEvent,
    BeatmapLightTranslationEventBox,
    BeatmapLightTranslationEventData,
    BeatmapLightTranslationEventContainer,
    LightTranslationEventsContainer,
    TranslationEvent
    >
{
    public override void SetLightEventAppearance(EventAppearanceSO so, BeatmapLightTranslationEventContainer con, float time, int i)
        => so.SetLightTranslationEventAppearance(con, i);
}
