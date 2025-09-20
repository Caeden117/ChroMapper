using Beatmap.Base;
using UnityEngine;

public class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private InterscopeRingLaserManager[] laserManagers;

    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index)
    {
        base.HandlePositionEvent(state, evt, index);
        base.HandleRotationEvent(state, evt, index);
        foreach (var isRingLaserManager in laserManagers) isRingLaserManager.HandlePositionEvent(state, evt, index);
    }

    public override void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index)
    {
        // Do nothing
    }
}
