using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapLightRotationEventContainer : BeatmapLightEventContainerBase<
    BeatmapLightRotationEvent,
    BeatmapLightRotationEventBox,
    BeatmapLightRotationEventData,
    BeatmapLightRotationEventContainer,
    LightRotationEventsContainer,
    RotatingEvent
    >
{
    [SerializeField] private MeshRenderer axisMark;

    public void SetRotationAxisAppearance(int axis)
    {
        axisMark.transform.localRotation = Quaternion.Euler(
            axis == 1 ? 90 : 0,
            axis == 0 ? 90 : 0,
            0
            );
    }

    public override void SetLightEventAppearance(EventAppearanceSO so, BeatmapLightRotationEventContainer con, float time, int i)
    {
        so.SetLightRotationEventAppearance(con, i);
    }
}
