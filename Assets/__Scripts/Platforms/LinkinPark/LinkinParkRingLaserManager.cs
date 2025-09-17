using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

internal class LinkinParkRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<RotatingLightsRandom> lpLasers;
    [SerializeField] private bool SwitchOnZoom;

    public override Object[] GetToDestroy() => new Object[] { this };

    public override void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index)
    {
        if (SwitchOnZoom)
            TriggerSwitch(index % 2 == 1);
    }

    public override void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index)
    {
        if (!SwitchOnZoom)
            TriggerSwitch(index % 2 == 1);
    }

    private void TriggerSwitch(bool b) => lpLasers.ForEach(it => it.SwitchStyle(b));
}
