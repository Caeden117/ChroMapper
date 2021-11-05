using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeTime = 2f;
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);
    public static readonly float HDRFlashIntensity = Mathf.GammaToLinearSpace(3);

    [FormerlySerializedAs("disableCustomInitialization")] public bool DisableCustomInitialization;

    public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    public LightGroup[] LightsGroupedByZ = { };
    public List<RotatingLightsBase> RotatingLights = new List<RotatingLightsBase>();

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    private int previousValue;

    private IEnumerator Start()
    {
        yield return null;
        LoadOldLightOrder();
    }

    public void LoadOldLightOrder()
    {
        if (!DisableCustomInitialization)
        {
            foreach (var e in GetComponentsInChildren<LightingEvent>())
            {
                // No, stop that. Enforcing Light ID breaks Glass Desert
                if (!e.OverrideLightGroup)
                    ControllingLights.Add(e);
            }

            foreach (var e in GetComponentsInChildren<RotatingLightsBase>())
            {
                if (!e.IsOverrideLightGroup())
                    RotatingLights.Add(e);
            }

            var lightIdOrder = ControllingLights.OrderBy(x => x.LightID).GroupBy(x => x.LightID).Select(x => x.First())
                .ToList();
            LightIDPlacementMap = lightIdOrder.ToDictionary(x => lightIdOrder.IndexOf(x), x => x.LightID);
            LightIDPlacementMapReverse = lightIdOrder.ToDictionary(x => x.LightID, x => lightIdOrder.IndexOf(x));

            LightsGroupedByZ = GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    public LightGroup[] GroupLightsBasedOnZ() => ControllingLights
        .Where(x => x.gameObject.activeInHierarchy)
        .GroupBy(x => Mathf.RoundToInt(x.PropGroup))
        .OrderBy(x => x.Key)
        .Select(x => new LightGroup { Lights = x.ToList() })
        .ToArray();

    public void ChangeAlpha(float alpha, float time, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateTargetAlpha(alpha, time);
    }

    public void ChangeMultiplierAlpha(float alpha, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateMultiplyAlpha(alpha);
    }

    public void ChangeColor(Color color, float time, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateTargetColor(color * HDRIntensity, time);
    }

    public void Fade(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * HDRFlashIntensity, 0);
            if (light.CanBeTurnedOff)
            {
                light.UpdateTargetAlpha(0, FadeTime);
                light.UpdateTargetColor(Color.black, FadeTime);
            }
            else
            {
                light.UpdateTargetColor(color * HDRIntensity, FadeTime);
            }
        }
    }

    public void Flash(Color color, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights)
        {
            light.UpdateTargetAlpha(1, 0);
            light.UpdateTargetColor(color * HDRFlashIntensity, 0);
            light.UpdateTargetColor(color * HDRIntensity, FadeTime);
        }
    }

    public void SetValue(int value)
    {
        // Ignore Chroma 1.0 values
        if (value < 0xff) previousValue = value;
    }

    public void Boost(bool boost, Color a, Color b)
    {
        // Off
        if (previousValue == 0) return;

        if (previousValue <= 3) a = b;

        foreach (var light in ControllingLights)
        {
            light.UpdateBoostState(boost);
            if (!light.UseInvertedPlatformColors)
                SetTargets(light, a);
        }
    }

    private void SetTargets(LightingEvent light, Color a)
    {
        if (previousValue == MapEvent.LightValueBlueFade || previousValue == MapEvent.LightValueRedFade)
        {
            light.UpdateCurrentColor(a * HDRFlashIntensity);
            light.UpdateTargetAlpha(0);
        }
        else
        {
            light.UpdateTargetColor(a * HDRIntensity, 0);
            light.UpdateTargetAlpha(a.a);
        }
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.red;
    //    if (GroupingMultiplier <= 0.1f) return;
    //    for (var i = -5; i < 150; i++)
    //    {
    //        var z = ((i - GroupingOffset) / GroupingMultiplier) + 0.5f;
    //        Gizmos.DrawLine(new Vector3(-50, 0, z), new Vector3(50, 0, z));
    //    }
    //}

    [Serializable]
    public class LightGroup
    {
        public List<LightingEvent> Lights = new List<LightingEvent>();
    }
}
