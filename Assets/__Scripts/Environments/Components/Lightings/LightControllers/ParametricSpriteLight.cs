using System;
using UnityEngine;

public class ParametricSpriteLight : MonoBehaviour
{
    public Renderer Renderer;

    public float WidthMultiplier = 1f;
    public float Width = 0.5f;
    public float Length = 1f;
    public float Center = 0.5f;
    public float AlphaMultiplier = 1f;
    public float MinAlpha;

    public float AlphaStart = 1f;
    public float AlphaEnd = 1f;
    public float WidthStart = 1f;
    public float WidthEnd = 1f;

    public bool UseCollision;
    public float CollisionLength;

    private MaterialPropertyBlock mpb;
    private bool hasInitialized;
    private Color color;
    private static readonly int colorId = Shader.PropertyToID("_Color");
    private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");
    private static readonly int alphaWidthId = Shader.PropertyToID("_AlphaWidth");

    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        hasInitialized = false;
        color = new Color(0f, 0.5f, 1f);
        Start();
    }

    private void Start()
    {
        if (hasInitialized)
            SetColor(color);
        else
            InitIfNeeded();
    }

    public void InitIfNeeded()
    {
        if (hasInitialized) return;
        mpb ??= new MaterialPropertyBlock();
        hasInitialized = Renderer != null;
        SetColor(color);
    }

    public void SetColor(Color col)
    {
        color = col;
        if (!hasInitialized) return;

        var length = UseCollision ? Mathf.Min(CollisionLength, Length) : Length;
        var alphaEnd = Mathf.Lerp(AlphaStart, AlphaEnd, Mathf.InverseLerp(0f, Length, length));

        color.a *= AlphaMultiplier;
        color.a = Mathf.Max(color.a, MinAlpha);
        mpb.SetColor(colorId, color);
        mpb.SetVector(alphaWidthId, new Vector4(AlphaStart, alphaEnd, WidthStart, WidthEnd));
        mpb.SetVector(sizeParamsId, new Vector4(Width * WidthMultiplier, length, Center, Width * 2f * WidthMultiplier));
        Renderer.SetPropertyBlock(mpb);
    }
}
