using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

class LPRingLaserManager : TrackLaneRingsManagerBase
{

    [SerializeField] private List<RotatingLightsRandom> lpLasers;

    public override Object[] GetToDestroy()
    {
        return new Object[] { this };
    }

    public override void HandlePositionEvent(JSONNode customData = null)
    {
        
    }

    public override void HandleRotationEvent(JSONNode customData = null)
    {
        lpLasers.ForEach(it => it.SwitchStyle());
    }
}
