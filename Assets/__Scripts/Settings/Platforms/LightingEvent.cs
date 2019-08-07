using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingEvent : MonoBehaviour {

    public static readonly float FadeOutTime = 1f;
    public static readonly float FlashTime = 0.05f;

    [HideInInspector] public Material LightMaterial;

    float initialOutlineAlphaRatio = 0;

    private Coroutine alphaCoroutine = null;
    private Coroutine colorCoroutine = null;

	// Use this for initialization
	IEnumerator Start () {
        LightMaterial = GetComponent<Renderer>().material;
        initialOutlineAlphaRatio = LightMaterial.GetColor("_FirstOutlineColor").a;
        yield return StartCoroutine(changeAlpha(0));
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
        Color Outline = LightMaterial.GetColor("_FirstOutlineColor"); 
        float t = 0;
        while (t <= time)
        {
            t += Time.deltaTime;
            float newAlpha = Mathf.Lerp(Main.a, Alpha, t / time);
            Main.a = newAlpha;
            Outline.a = Main.a * initialOutlineAlphaRatio;
            LightMaterial.SetColor("_Color", Main);
            LightMaterial.SetColor("_FirstOutlineColor", Outline);
            yield return new WaitForEndOfFrame();
        }
        Main.a = Alpha;
        Outline.a = Main.a * initialOutlineAlphaRatio;
        LightMaterial.SetColor("_Color", Main);
        LightMaterial.SetColor("_FirstOutlineColor", Outline);
        alphaCoroutine = null;
    }

    public IEnumerator changeColor(Color color, float time = 0)
    {
        Color Main = LightMaterial.GetColor("_Color");
        Color Outline = LightMaterial.GetColor("_FirstOutlineColor");
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            Main = Color.Lerp(Main, color, t / time);
            Outline = Main;
            Outline.a = Main.a * initialOutlineAlphaRatio;
            LightMaterial.SetColor("_Color", Main);
            LightMaterial.SetColor("_FirstOutlineColor", Outline);
            yield return new WaitForEndOfFrame();
        }
        Main = color;
        Outline = color;
        Outline.a = Main.a * initialOutlineAlphaRatio;
        LightMaterial.SetColor("_Color", Main);
        LightMaterial.SetColor("_FirstOutlineColor", Outline);
        colorCoroutine = null;
    }
}
