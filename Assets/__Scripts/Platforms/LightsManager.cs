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

    public List<int> EditorToGamePropIDMap = new List<int>();
    public List<int> EditorToGameLightIDMap = new List<int>();

    public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    public LightGroup[] LightsGroupedByZ = new LightGroup[] { };
    public List<RotatingLightsBase> RotatingLights = new List<RotatingLightsBase>();

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        // Multiple CM prop ids could align with the same game prop ids
        EditorToGamePropIDMap = EditorToGamePropIDMap.Distinct().ToList();
        EditorToGameLightIDMap = EditorToGameLightIDMap.Distinct().ToList();
        LoadOldLightOrder();
    }

    public void LoadOldLightOrder()
    {
        if (!disableCustomInitialization)
        {
            foreach (LightingEvent e in GetComponentsInChildren<LightingEvent>())
            {
                if (!e.OverrideLightGroup)
                {
                    ControllingLights.Add(e);
                }
            }
            ControllingLights = ControllingLights.OrderBy(x => x.transform.position.z + x.lightIdOffset).ToList();
            foreach (RotatingLightsBase e in GetComponentsInChildren<RotatingLightsBase>())
            {
                if (!e.IsOverrideLightGroup())
                {
                    RotatingLights.Add(e);
                }
            }
            LightsGroupedByZ = GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    // NEEDED FOR CM RING PROP TO BASE GAME TRANSLATION
    public LightGroup[] GroupLightsBasedOnZ()
    {
        Dictionary<int, List<LightingEvent>> pregrouped = new Dictionary<int, List<LightingEvent>>();
        foreach(LightingEvent light in ControllingLights)
        {
            if (!light.gameObject.activeInHierarchy) continue;
            float tz = (light.transform.position.z * GroupingMultiplier) + GroupingOffset;
            int z = Mathf.RoundToInt(tz);
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
        var grouped = new LightGroup[pregrouped.Count];
        //We gotta squeeze the distance between Z positions into a nice 0-1-2-... array
        int i = 0;
        foreach (var group in pregrouped.OrderBy(it => it.Key).Select(it => it.Value))
        {
            if (group is null) continue;
            grouped[i] = new LightGroup
            {
                Lights = group.ToList()
            };
            i++;
        }

        return grouped;
    }

    public void ChangeAlpha(float Alpha, float time, IEnumerable<LightingEvent> lights)
    {
        foreach(LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(Alpha, time);
        }
    }

    public void ChangeMultiplierAlpha(float Alpha, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateMultiplyAlpha(Alpha);
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
            light.UpdateTargetAlpha(1, 0);
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
            light.UpdateTargetAlpha(1, 0);
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
            a = b;
        }

        foreach (LightingEvent light in ControllingLights)
        {
            if (!light.UseInvertedPlatformColors)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (GroupingMultiplier <= 0.1f) return;
        for (var i = -5; i < 150; i++)
        {
            var z = ((i - GroupingOffset) / GroupingMultiplier) + 0.5f;
            Gizmos.DrawLine(new Vector3(-50, 0, z), new Vector3(50, 0, z));
        }
    }

    [System.Serializable]
    public class LightGroup
    {
        public List<LightingEvent> Lights = new List<LightingEvent>();
    }
}
