using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingEvent : MonoBehaviour {

    public static readonly float FadeOutTime = 1f;
    public static readonly float FlashTime = 0.05f;
    public static readonly float HDR_Intensity = 2.4169f;

    [HideInInspector] public Material LightMaterial;

    private Coroutine alphaCoroutine = null;
    private Coroutine colorCoroutine = null;

	// Use this for initialization
	void Start () {
        LightMaterial = GetComponent<Renderer>().material;
        ChangeAlpha(0);
	}

    public void ChangeAlpha(float Alpha, float time = 0)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        alphaCoroutine = StartCoroutine(changeAlpha(Alpha, time));
    }

    public void ChangeColor(Color color, float time = 0)
    {
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(changeColor(color, time));
    }

    public IEnumerator Fade(Color color)
    {
        ChangeColor(color);
        yield return colorCoroutine;
        ChangeAlpha(0, FadeOutTime);
        yield return alphaCoroutine;
    }

    public IEnumerator changeAlpha(float Alpha, float time = 0)
    {
        Color Main = LightMaterial.GetColor("_Color");
        float oldAlpha = Main.a;
        float t = 0;
        while (t <= time)
        {
            t += Time.deltaTime;
            float newAlpha = Mathf.Lerp(oldAlpha, Alpha, t / time);
            Main.a = newAlpha;
            LightMaterial.SetColor("_Color", Main);
            yield return new WaitForEndOfFrame();
        }
        Main.a = Alpha;
        LightMaterial.SetColor("_Color", Main);
        alphaCoroutine = null;
    }

    public IEnumerator changeColor(Color color, float time = 0)
    {
        Color modified = color * Mathf.GammaToLinearSpace(HDR_Intensity);
        Color Outline = LightMaterial.GetColor("_EmissionColor");
        Color original = Outline;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            Outline = Color.Lerp(original, modified, t / time);
            LightMaterial.SetColor("_EmissionColor", Outline);
            yield return new WaitForEndOfFrame();
        }
        Outline = modified;
        LightMaterial.SetColor("_EmissionColor", Outline);
        colorCoroutine = null;
    }
}
