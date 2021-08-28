using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

internal class IsRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<MovingLightsRandom> isLasers;

    public override Object[] GetToDestroy() => new Object[] {this};

    public override void HandlePositionEvent(JSONNode customData = null) => isLasers.ForEach(it => it.SwitchStyle());

    public override void HandleRotationEvent(JSONNode customData = null)
    {
    }
}
