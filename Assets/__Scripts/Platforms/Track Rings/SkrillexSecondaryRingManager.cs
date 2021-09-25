using SimpleJSON;
using UnityEngine;

public class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private InterscopeRingLaserManager[] laserManagers;

    public override void HandlePositionEvent(JSONNode customData = null)
    {
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
