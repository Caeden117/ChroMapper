using Beatmap.Base;
using UnityEngine;

public class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private InterscopeRingLaserManager[] laserManagers;

    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(BaseEvent evt)
    {
        base.HandlePositionEvent(evt);
        base.HandleRotationEvent(evt);
        foreach (var isRingLaserManager in laserManagers)
        {
            isRingLaserManager.HandlePositionEvent(evt);
        }
    }

    public override void HandleRotationEvent(BaseEvent evt)
    {
        // Do nothing
    }
}
