using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

internal class InterscopeRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<MovingLightsRandom> isLasers;

    public override Object[] GetToDestroy() => new Object[] { this };

    public override void HandlePositionEvent(BaseEvent evt) => isLasers.ForEach(it => it.SwitchStyle());

    public override void HandleRotationEvent(BaseEvent evt)
    {
    }
}
