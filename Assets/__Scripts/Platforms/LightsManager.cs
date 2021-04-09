using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeTime = 2f;
    public static readonly float HDR_Intensity = Mathf.GammaToLinearSpace(2.4169f);
    public static readonly float HDR_Flash_Intensity = Mathf.GammaToLinearSpace(3);

    public bool disableCustomInitialization = false;
    private int previousValue = 0;

    public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    public LightGroup[] LightsGroupedByZ = new LightGroup[] { };
    public List<RotatingLightsBase> RotatingLights = new List<RotatingLightsBase>();

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    private IEnumerator Start()
    {
        yield return null;
        LoadOldLightOrder();
    }

    public void LoadOldLightOrder()
    {
        if (!disableCustomInitialization)
        {
            foreach (LightingEvent e in GetComponentsInChildren<LightingEvent>())
            {
                // No, stop that. Enforcing Light ID breaks Glass Desert
                if (!e.OverrideLightGroup)
                {
                    ControllingLights.Add(e);
                }
            }
            foreach (RotatingLightsBase e in GetComponentsInChildren<RotatingLightsBase>())
            {
                if (!e.IsOverrideLightGroup())
                {
                    RotatingLights.Add(e);
                }
            }

            var lightIdOrder = ControllingLights.OrderBy(x => x.lightID).GroupBy(x => x.lightID).Select(x => x.First()).ToList();
            LightIDPlacementMap = lightIdOrder.ToDictionary(x => lightIdOrder.IndexOf(x), x => x.lightID);
            LightIDPlacementMapReverse = lightIdOrder.ToDictionary(x => x.lightID, x => lightIdOrder.IndexOf(x));

            LightsGroupedByZ = GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    public LightGroup[] GroupLightsBasedOnZ() => ControllingLights
        .Where(x => x.gameObject.activeInHierarchy)
        .GroupBy(x => Mathf.RoundToInt(x.propGroup))
        .OrderBy(x => x.Key)
        .Select(x => new LightGroup { Lights = x.ToList() })
        .ToArray();

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
            light.UpdateTargetColor(color * HDR_Intensity, time);
        }
    }

    public void Fade(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * HDR_Flash_Intensity, 0);
            if (light.CanBeTurnedOff)
            {
                light.UpdateTargetAlpha(0, FadeTime);
                light.UpdateTargetColor(Color.black, FadeTime);
            }
            else
            {
                light.UpdateTargetColor(color * HDR_Intensity, FadeTime);
            }
        }
    }

    public void Flash(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (LightingEvent light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * HDR_Flash_Intensity, 0);
            light.UpdateTargetColor(color * HDR_Intensity, FadeTime);
        }
    }

    public void SetValue(int value)
    {
        // Ignore Chroma 1.0 values
        if (value < 0xff) previousValue = value;
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
            light.UpdateCurrentColor(a * HDR_Flash_Intensity);
            light.UpdateTargetAlpha(0);
        }
        else
        {
            light.UpdateTargetColor(a * HDR_Intensity, 0);
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
