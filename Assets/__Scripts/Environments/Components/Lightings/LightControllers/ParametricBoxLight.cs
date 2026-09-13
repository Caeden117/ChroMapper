using System;
using UnityEngine;

public class ParametricBoxLight : MonoBehaviour
{
    public Renderer Renderer;

    public float AlphaStart = 1f;
    public float AlphaEnd = 1f;
    public float AlphaMultiplier = 1f;
    public float Width = 1f;
    public float WidthStart = 1f;
    public float WidthEnd = 1f;
    public float Center = 0.5f;
    public float Height = 1f;
    public float Length = 1f;
    public float MinAlpha;

    public bool UseCollision;
    public float CollisionHeight;
    public bool UpdateTransform = true;

    private Transform tr;
    private MaterialPropertyBlock mpb;
    private Color color;
    private bool hasInitialized;
    private static readonly int colorId = Shader.PropertyToID("_Color");
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
        tr = transform;
        mpb ??= new MaterialPropertyBlock();
        hasInitialized = Renderer != null;
        SetColor(color);
    }

    public void SetColor(Color col)
    {
        color = col;
        if (!hasInitialized) return;

        var height = UseCollision ? Mathf.Min(CollisionHeight, Height) : Height;
        if (UpdateTransform)
        {
            tr.localScale = new Vector3(Width * 0.5f, height * 0.5f, Length * 0.5f);
            tr.localPosition = new Vector3(0f, (0.5f - Center) * height, 0f);
        }

        var newCol = color;
        newCol.a *= AlphaMultiplier;
        if (newCol.a < MinAlpha) newCol.a = MinAlpha;

        var alphaEnd = Mathf.Lerp(AlphaStart, AlphaEnd, Mathf.InverseLerp(0f, Height, height));
        mpb.SetColor(colorId, newCol);
        mpb.SetVector(alphaWidthId, new Vector4(AlphaStart, alphaEnd, WidthStart, WidthEnd));
        Renderer.SetPropertyBlock(mpb);
    }
}
