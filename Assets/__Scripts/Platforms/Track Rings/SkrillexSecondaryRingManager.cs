using SimpleJSON;
using UnityEngine;

public class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private InterscopeRingLaserManager[] laserManagers;

    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(JSONNode customData = null)
    {
        base.HandlePositionEvent(customData);
        base.HandleRotationEvent(customData);
        foreach (var isRingLaserManager in laserManagers)
        {
            isRingLaserManager.HandlePositionEvent(customData);
        }
    }

    public override void HandleRotationEvent(JSONNode customData = null)
    {
        // Do nothing
    }
}
