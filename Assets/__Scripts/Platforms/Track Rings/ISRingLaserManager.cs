using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

class ISRingLaserManager : TrackLaneRingsManagerBase
{

    [SerializeField] private List<MovingLightsRandom> isLasers;

    public override Object[] GetToDestroy()
    {
        return new Object[] { this };
    }

    public override void HandlePositionEvent()
    {
        isLasers.ForEach(it => it.SwitchStyle());
    }

    public override void HandleRotationEvent(JSONNode customData = null)
    {

    }
}
