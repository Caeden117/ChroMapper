using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

internal class InterscopeRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<MovingLightsRandom> isLasers;
    
    public override Object[] GetToDestroy() => new Object[] { this };

    public override void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index) =>
        isLasers.ForEach(it => it.SwitchStyle(index % 2 == 0));

    public override void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index)
    {
    }
}
