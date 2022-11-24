using System;
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
    [SerializeField] private GameObject axisMark;

    public override void SetLightEventAppearance(EventAppearanceSO so, BeatmapLightTranslationEventContainer con, float time, int i)
        => so.SetLightTranslationEventAppearance(con, i);
    internal void SetTranslationAxisAppearance(int axis)
    {
        axisMark.transform.localRotation = Quaternion.Euler(
            axis == 1 ? 90 : 0,
            axis == 0 ? 90 : 0,
            0
            );
    }
}
