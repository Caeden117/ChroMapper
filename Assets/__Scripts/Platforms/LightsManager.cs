using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeOutTime = 1f;
    public static readonly float FlashTime = 0.05f;
    public static readonly float HDR_Intensity = 2.4169f;

    [HideInInspector] public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    [HideInInspector] public List<RotatingLights> RotatingLights = new List<RotatingLights>();

    private Coroutine alphaCoroutine = null;
    private Coroutine colorCoroutine = null;
    private Dictionary<TrackLaneRing, Coroutine> ringAlphas = new Dictionary<TrackLaneRing, Coroutine>(); //For ring prop
    private Dictionary<TrackLaneRing, Coroutine> ringColors = new Dictionary<TrackLaneRing, Coroutine>(); //ONLY

    private void Awake()
    {
        SceneTransitionManager.Instance.AddLoadRoutine(LoadLights());
    }

    IEnumerator LoadLights()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (LightingEvent e in GetComponentsInChildren<LightingEvent>()) ControllingLights.Add(e);
        foreach (RotatingLights e in GetComponentsInChildren<RotatingLights>()) RotatingLights.Add(e);
        ChangeAlpha(0);
    }

    public void ChangeAlpha(float Alpha, float time = 0, TrackLaneRing ring = null)
    {
        if (ring is null)
        {
            if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            if (time > 0) alphaCoroutine = StartCoroutine(changeAlpha(Alpha, time));
            else UpdateColor(Color.white * Alpha, false, null);
        }
        else
        {
            if (ringAlphas.TryGetValue(ring, out Coroutine alphaR) && alphaR != null) StopCoroutine(alphaR);
            List<LightingEvent> filteredEvents = ring.gameObject.GetComponentsInChildren<LightingEvent>().ToList();
            if (time > 0) ringAlphas[ring] = StartCoroutine(changeAlpha(Alpha, time, filteredEvents));
            else UpdateColor(Color.white * Alpha, false, filteredEvents);
        }
    }

    public void ChangeColor(Color color, float time = 0, TrackLaneRing ring = null)
    {
        if (ring is null)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            if (time > 0) colorCoroutine = StartCoroutine(changeColor(color, time));
            else UpdateColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), true, null);
        }
        else
        {
            if (ringColors.TryGetValue(ring, out Coroutine colorR) && colorR != null) StopCoroutine(colorR);
            List<LightingEvent> filteredEvents = ring.gameObject.GetComponentsInChildren<LightingEvent>().ToList();
            if (time > 0) ringColors[ring] = StartCoroutine(changeColor(color, time, filteredEvents));
            else UpdateColor(color * Mathf.GammaToLinearSpace(HDR_Intensity), true, filteredEvents);
        }
    }

    public void Fade(Color color, TrackLaneRing ring = null)
    {
        if (ring is null)
        {
            if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            UpdateColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), true, null);
            UpdateColor(Color.white, false, null);
            colorCoroutine = StartCoroutine(changeColor(Color.black, FadeOutTime, null));
            alphaCoroutine = StartCoroutine(changeAlpha(0, FadeOutTime, null));
        }
        else
        {
            if (ringAlphas.TryGetValue(ring, out Coroutine alphaR) && alphaR != null) StopCoroutine(alphaR);
            if (ringColors.TryGetValue(ring, out Coroutine colorR) && colorR != null) StopCoroutine(colorR);
            List<LightingEvent> filteredEvents = ring.gameObject.GetComponentsInChildren<LightingEvent>().ToList();
            UpdateColor(color * Mathf.GammaToLinearSpace(Mathf.Ceil(HDR_Intensity)), true, filteredEvents);
            UpdateColor(Color.white, false, filteredEvents);
            ringColors[ring] = StartCoroutine(changeColor(Color.black, FadeOutTime, filteredEvents));
            ringAlphas[ring] = StartCoroutine(changeAlpha(0, FadeOutTime, filteredEvents));
        }
    }

    public IEnumerator changeAlpha(float Alpha, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        float oldAlpha = (filteredEvents == null ? ControllingLights.First().LightMaterial : filteredEvents.First().LightMaterial)
            .GetColor("_Color").a;
        float t = 0;
        while (t <= time)
        {
            t += Time.deltaTime;
            float lerpedAlpha = Mathf.Lerp(oldAlpha, Alpha, t / time);
            UpdateColor(Color.white * lerpedAlpha, false, filteredEvents);
            yield return new WaitForEndOfFrame();
        }
        UpdateColor(Color.white * Alpha, false, filteredEvents);
    }

    public IEnumerator changeColor(Color color, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        Color modified = color * Mathf.GammaToLinearSpace(HDR_Intensity);
        Color Outline = (filteredEvents == null ? ControllingLights.First().LightMaterial : filteredEvents.First().LightMaterial)
            .GetColor("_EmissionColor");
        Color original = Outline;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            Outline = Color.Lerp(original, modified, t / time);
            UpdateColor(Outline, true, filteredEvents);
            yield return new WaitForEndOfFrame();
        }
        Outline = modified;
        UpdateColor(Outline, true, filteredEvents);
    }

    private void UpdateColor(Color color, bool emissive, List<LightingEvent> filteredEvents = null)
    {
        if (filteredEvents is null) //Welcome to Python.
            foreach (LightingEvent e in ControllingLights)
                e.LightMaterial.SetColor(emissive ? "_EmissionColor" : "_Color", color);
        else foreach(LightingEvent e in filteredEvents)
            e.LightMaterial.SetColor(emissive ? "_EmissionColor" : "_Color", color);
    }
}
