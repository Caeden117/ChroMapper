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
    public bool ZRotatable = false;

    public List<RotatingEvent> ControllingRotations = new List<RotatingEvent>();

    protected new IEnumerator Start()
    {
        yield return base.Start();
        var lights = GetComponentsInChildren<LightingEvent>();
        if (DisableCustomInitialization) // we reuse this bool option, if it is true, we will only add those lightingEvents having same group id
        {
            int cnt = 0;
            foreach (var light in lights)
            {
                if (light.OverrideLightGroupID == GroupId)
                {
                    ControllingLights.Add(light);
                    light.LightIdx = cnt;
                    cnt++;
                }
            }
        }
        else // all the lights are belonged to this group.
        {
            for (int i = 0; i < lights.Length; ++i)
            {
                lights[i].LightIdx = i;
            }
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
        if (ZRotatable) XRotatable = true; // for compatibility with sanity check
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
            light.SetTargetColor((light.TargetColorId == 0 ? red : blue) * HDRIntensity);
        }
    }
}
