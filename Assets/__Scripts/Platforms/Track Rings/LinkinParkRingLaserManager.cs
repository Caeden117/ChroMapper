using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

internal class LinkinParkRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<RotatingLightsRandom> lpLasers;

    public override Object[] GetToDestroy() => new Object[] {this};

    public override void HandlePositionEvent(JSONNode customData = null)
    {
    }

    public override void HandleRotationEvent(JSONNode customData = null) => lpLasers.ForEach(it => it.SwitchStyle());
}
