using SimpleJSON;
using UnityEngine;

class SkrillexSecondaryRingManager : TrackLaneRingsManager
{
    [SerializeField] private ISRingLaserManager[] laserManagers;

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
