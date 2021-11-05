using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

internal class LinkinParkRingLaserManager : TrackLaneRingsManagerBase
{
    [SerializeField] private List<RotatingLightsRandom> lpLasers;
    [SerializeField] private bool SwitchOnZoom;

    public override Object[] GetToDestroy() => new Object[] { this };

    public override void HandlePositionEvent(JSONNode customData = null)
    {
        if (SwitchOnZoom)
            TriggerSwitch();
    }

    public override void HandleRotationEvent(JSONNode customData = null)
    {
        if (!SwitchOnZoom)
            TriggerSwitch();
    }

    private void TriggerSwitch() => lpLasers.ForEach(it => it.SwitchStyle());
}
