using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsManager : MonoBehaviour
{
    public static readonly float FadeOutTime = 1f;
    public static readonly float FlashTime = 0.05f;
    public static readonly float HDR_Intensity = 2.4169f;

    [HideInInspector] public List<LightingEvent> ControllingLights = new List<LightingEvent>();
    [HideInInspector] public List<RotatingLights> RotatingLights = new List<RotatingLights>();

    private Color mainColor = Color.white;
    private Color emissiveColor = new Color(0, 2.151823f, 4.237095f);
    private Coroutine alphaCoroutine = null;
    private Coroutine colorCoroutine = null;

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

    public void ChangeAlpha(float Alpha, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        if (time > 0) alphaCoroutine = StartCoroutine(changeAlpha(Alpha, time));
        else
        {
            mainColor.a = Alpha;
            UpdateColor(mainColor, false, filteredEvents);
        }
    }

    public void ChangeColor(Color color, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        if (time > 0) colorCoroutine = StartCoroutine(changeColor(color, time));
        else
        {
            emissiveColor = color * Mathf.GammaToLinearSpace(HDR_Intensity);
            UpdateColor(emissiveColor, true, filteredEvents);
        }
    }

    public void Fade(Color color, List<LightingEvent> filteredEvents = null)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        emissiveColor = color * Mathf.GammaToLinearSpace(HDR_Intensity);
        mainColor = Color.white;
        UpdateColor(emissiveColor, true, filteredEvents);
        UpdateColor(mainColor, false, filteredEvents);
        colorCoroutine = StartCoroutine(changeColor(Color.black, FadeOutTime));
        alphaCoroutine = StartCoroutine(changeAlpha(0, FadeOutTime));
    }

    public IEnumerator changeAlpha(float Alpha, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        float oldAlpha = mainColor.a;
        float t = 0;
        while (t <= time)
        {
            t += Time.deltaTime;
            float newAlpha = Mathf.Lerp(oldAlpha, Alpha, t / time);
            mainColor.a = newAlpha;
            UpdateColor(mainColor, false, filteredEvents);
            yield return new WaitForEndOfFrame();
        }
        mainColor.a = Alpha;
        UpdateColor(mainColor, false, filteredEvents);
        alphaCoroutine = null;
    }

    public IEnumerator changeColor(Color color, float time = 0, List<LightingEvent> filteredEvents = null)
    {
        Color modified = color * Mathf.GammaToLinearSpace(HDR_Intensity);
        Color Outline = emissiveColor;
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
        colorCoroutine = null;
    }

    private void UpdateColor(Color color, bool emissive, List<LightingEvent> filteredEvents = null)
    {
        if (filteredEvents is null)
        {
            foreach (LightingEvent e in ControllingLights)
                e.LightMaterial.SetColor(emissive ? "_EmissionColor" : "_Color", color);
        }else foreach(LightingEvent e in filteredEvents)
                e.LightMaterial.SetColor(emissive ? "_EmissionColor" : "_Color", color);
    }
}
