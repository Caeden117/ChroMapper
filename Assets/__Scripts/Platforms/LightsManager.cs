using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeTime = 2f;
    public static readonly float HDR_Intensity = 2.4169f;

    public bool disableCustomInitialization = false;

    [HideInInspector] public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    [HideInInspector] public LightingEvent[][] LightsGroupedByZ = new LightingEvent[][] { };
    [HideInInspector] public List<RotatingLights> RotatingLights = new List<RotatingLights>();

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
            SceneTransitionManager.Instance.AddLoadRoutine(LoadLights());
        else StartCoroutine(LoadLights());
    }

    IEnumerator LoadLights()
    {
        if (this == null)
            yield break;
        if (!disableCustomInitialization)
        {
            foreach (LightingEvent e in GetComponentsInChildren<LightingEvent>())
            {
                if (!e.OverrideLightGroup)
                {
                    ControllingLights.Add(e);
                }
            }
            foreach (RotatingLights e in GetComponentsInChildren<RotatingLights>())
            {
                if (!e.OverrideLightGroup)
                {
                    RotatingLights.Add(e);
                }
            }
            GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
            ChangeAlpha(0, 0, ControllingLights);
    }

    //Needed for Ring Prop to work.
    public void GroupLightsBasedOnZ()
    {
        var pregrouped = ControllingLights.ToLookup(x => Mathf.RoundToInt(x.transform.position.z));
        //The keys for this ILookup is base on actual Z position, not ideal.
        LightsGroupedByZ = new LightingEvent[pregrouped.Count][];
        //We gotta squeeze the distance between Z positions into a nice 0-1-2-... array
        int i = 0;
        foreach (var group in pregrouped)
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
    {foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), 0);
            light.UpdateTargetAlpha(0, FadeTime);
            light.UpdateTargetColor(Color.black, FadeTime);
        }
    }

    public void Flash(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), 0);
            light.UpdateTargetColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), FadeTime);
        }
    }
}
