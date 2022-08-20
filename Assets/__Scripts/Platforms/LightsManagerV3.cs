using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsManagerV3 : LightsManager
{
    [Header("V3 Configurations")]
    public int GroupId;
    public bool XRotatable = true;
    public bool YRotatable = false;
    public bool XFlip = false;
    public bool YFlip = false;

    public List<RotatingEvent> ControllingRotations = new List<RotatingEvent>();

    protected new IEnumerator Start()
    {
        yield return base.Start();
        var lights = GetComponentsInChildren<LightingEvent>();
        for (int i = 0; i < lights.Length; ++i)
        {
            lights[i].LightIdx = i;
        }

        foreach (var rot in GetComponentsInChildren<RotatingEvent>())
        {
            ControllingRotations.Add(rot);
            rot.lightsManager = this;
        }
    }

}
