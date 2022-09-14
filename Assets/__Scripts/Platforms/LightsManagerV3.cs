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

        var rotations = GetComponentsInChildren<RotatingEvent>();
        for (int i = 0; i < rotations.Length; ++i)
        {
            ControllingRotations.Add(rotations[i]);
            rotations[i].lightsManager = this;
            rotations[i].RotationIdx = i;
            rotations[i].XData.flip = XFlip;
            rotations[i].YData.flip = YFlip;
        }
        yield return null;
    }

    public void ResetNoteIndex()
    {
        foreach (var light in ControllingLights) light.SetNoteIndex(-1, true);
        foreach (var rot in ControllingRotations) rot.ResetNoteIndex();
    }

    public override void Boost(bool boost, Color red, Color blue)
    {
        foreach (var light in ControllingLights)
        {
            light.UpdateBoostState(boost);
            light.SetTargetColor((light.TargetColor == 0 ? red : blue) * HDRIntensity);
        }
    }
}
