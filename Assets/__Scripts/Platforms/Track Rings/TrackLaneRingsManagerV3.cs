using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackLaneRingsManagerV3 : TrackLaneRingsManager
{
    // Start is called before the first frame update
    public override void Start()
    {
        // for v3 lights, we only want zoom function left. We will not use generating/update rotation
        Rings = GetComponentsInChildren<TrackLaneRing>();
    }
}
