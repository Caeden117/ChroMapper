using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeTime = 2f;
    public static readonly float HDR_Intensity = 2.4169f;

    public bool disableCustomInitialization = false;
    private int previousValue = 0;

    [HideInInspector] public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    [HideInInspector] public LightingEvent[][] LightsGroupedByZ = new LightingEvent[][] { };
    [HideInInspector] public List<IRotatingLights> RotatingLights = new List<IRotatingLights>();

    private void Start()
    {
        StartCoroutine(LoadLights());
    }

    IEnumerator LoadLights()
    {
        if (this == null)
            yield break;
        yield return new WaitForEndOfFrame();
        if (!disableCustomInitialization)
        {
            foreach (LightingEvent e in GetComponentsInChildren<LightingEvent>())
            {
                if (!e.OverrideLightGroup)
                {
                    ControllingLights.Add(e);
                }
            }
            ControllingLights = ControllingLights.OrderBy(x => x.transform.position.z).ToList();
            foreach (IRotatingLights e in GetComponentsInChildren<IRotatingLights>())
            {
                if (!e.IsOverrideLightGroup())
                {
                    RotatingLights.Add(e);
                }
            }
            GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    //Needed for Ring Prop to work.
    public void GroupLightsBasedOnZ()
    {
        Dictionary<int, List<LightingEvent>> pregrouped = new Dictionary<int, List<LightingEvent>>();
        foreach(LightingEvent light in ControllingLights)
        {
            if (!light.gameObject.activeSelf) continue;
            int z = Mathf.RoundToInt(light.transform.position.z + 0.001f);
            if (pregrouped.TryGetValue(z, out List<LightingEvent> list))
            {
                list.Add(light);
            }
            else
            {
                list = new List<LightingEvent>();
                list.Add(light);
                pregrouped.Add(z, list);
            }
        }
        //The above is base on actual Z position, not ideal.
        LightsGroupedByZ = new LightingEvent[pregrouped.Count][];
        //We gotta squeeze the distance between Z positions into a nice 0-1-2-... array
        int i = 0;
        foreach (var group in pregrouped.Values)
        {
            if (group is null) continue;
            LightsGroupedByZ[i] = group.ToArray();
            i++;
        }
    }

    public void ChangeAlpha(float Alpha, float time, IEnumerable<LightingEvent> lights)
    {
        foreach(LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(Alpha, time);
        }
    }

    public void ChangeColor(Color color, float time, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), time);
        }
    }

    public void Fade(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(color.a, 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), 0);
            if (light.CanBeTurnedOff)
            {
                light.UpdateTargetAlpha(0, FadeTime);
                light.UpdateTargetColor(Color.black, FadeTime);
            }
            else
            {
                light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), FadeTime);
            }
        }
    }

    public void Flash(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(color.a, 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), FadeTime);
        }
    }

    public void SetValue(int value)
    {
        previousValue = value;
    }

    public void Boost(Color a, Color b)
    {
        // Off
        if (previousValue == 0) return;

        if (previousValue <= 3)
        {
            (a, b) = (b, a);
        }

        foreach (LightingEvent light in ControllingLights)
        {
            if (light.UseInvertedPlatformColors)
            {
                SetTargets(light, b);
            }
            else
            {
                SetTargets(light, a);
            }
        }
    }

    private void SetTargets(LightingEvent light, Color a)
    {
        if (previousValue == MapEvent.LIGHT_VALUE_BLUE_FADE || previousValue == MapEvent.LIGHT_VALUE_RED_FADE)
        {
            light.UpdateCurrentColor(a * Mathf.GammaToLinearSpace(HDR_Intensity));
            light.UpdateTargetAlpha(0);
        }
        else
        {
            light.UpdateTargetColor(a * Mathf.GammaToLinearSpace(HDR_Intensity), 0);
            light.UpdateTargetAlpha(a.a);
        }
    }
}
