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

    public void ChangeAlpha(float Alpha, float time = 0)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        if (time > 0)
            alphaCoroutine = StartCoroutine(changeAlpha(Alpha, time));
        else
        {
            mainColor.a = Alpha;
            UpdateColor(mainColor, false);
        }
    }

    public void ChangeColor(Color color, float time = 0)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        if (time > 0)
            colorCoroutine = StartCoroutine(changeColor(color, time));
        else
        {
            emissiveColor = color * Mathf.GammaToLinearSpace(HDR_Intensity);
            UpdateColor(emissiveColor);
        }
    }

    public void Fade(Color color)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        foreach (LightingEvent e in ControllingLights)
        {
            emissiveColor = color * Mathf.GammaToLinearSpace(HDR_Intensity);
            mainColor = Color.white;
            e.LightMaterial.SetColor("_EmissionColor", emissiveColor);
            e.LightMaterial.SetColor("_Color", mainColor);
        }
        colorCoroutine = StartCoroutine(changeColor(Color.black, FadeOutTime));
        alphaCoroutine = StartCoroutine(changeAlpha(0, FadeOutTime));
    }

    public IEnumerator changeAlpha(float Alpha, float time = 0)
    {
        float oldAlpha = mainColor.a;
        float t = 0;
        while (t <= time)
        {
            t += Time.deltaTime;
            float newAlpha = Mathf.Lerp(oldAlpha, Alpha, t / time);
            mainColor.a = newAlpha;
            UpdateColor(mainColor, false);
            yield return new WaitForEndOfFrame();
        }
        mainColor.a = Alpha;
        UpdateColor(mainColor, false);
        alphaCoroutine = null;
    }

    public IEnumerator changeColor(Color color, float time = 0)
    {
        Color modified = color * Mathf.GammaToLinearSpace(HDR_Intensity);
        Color Outline = emissiveColor;
        Color original = Outline;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            Outline = Color.Lerp(original, modified, t / time);
            UpdateColor(Outline);
            yield return new WaitForEndOfFrame();
        }
        Outline = modified;
        UpdateColor(Outline);
        colorCoroutine = null;
    }

    private void UpdateColor(Color color, bool emissive = true)
    {
        foreach (LightingEvent e in ControllingLights)
            e.LightMaterial.SetColor(emissive ? "_EmissionColor" : "_Color", color);
    }
}
