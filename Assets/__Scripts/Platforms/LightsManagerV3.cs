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
    public bool ZFlip = false;
    public bool TreatZAsX = false;


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

        if (ControllingRotations.Count == 0) // include all rotations
        {
            var rotations = GetComponentsInChildren<RotatingEvent>();
            for (int i = 0; i < rotations.Length; ++i)
            {
                ControllingRotations.Add(rotations[i]);
            }
        }
        for (int i = 0; i < ControllingRotations.Count; ++i)
        {
            ControllingRotations[i].lightsManager = this;
            ControllingRotations[i].RotationIdx = i;
            ControllingRotations[i].XData.flip = XFlip;
            ControllingRotations[i].YData.flip = YFlip;
            ControllingRotations[i].ZData.flip = ZFlip;
        }

        if (TreatZAsX) XRotatable = true; // for compatibility with sanity check
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
