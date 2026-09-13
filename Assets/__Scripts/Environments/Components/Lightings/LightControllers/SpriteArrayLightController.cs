using System;
using UnityEngine;

public class SpriteArrayLightController : LightController
{
    [SerializeField] public SpriteRenderer[] SpriteRenderers = Array.Empty<SpriteRenderer>();

    [SerializeField] public bool HideIfAlphaOutOfRange;
    [SerializeField] public float HideAlphaRangeMin = 0.001f;
    [SerializeField] public float HideAlphaRangeMax = 1f;

    [SerializeField] public float Intensity = 1f;
    [SerializeField] public float MinAlpha;
    [SerializeField] public MultiplyColorByAlphaType MultiplyColorByAlpha;
    [SerializeField] public bool SetColorOnly;
    [SerializeField] public bool SetAlphaOnly;
    [SerializeField] public bool SetOnlyOnce;

    public override bool IsPhysical => true;
    protected override bool Initialize() => true;

    public override void SetColor(Color color)
    {
        if (!HasInitialized) return;
        if (MultiplyColorByAlpha == MultiplyColorByAlphaType.BeforeApplyingMinAlpha)
        {
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        color.a = SetColorOnly ? SpriteRenderers[0].color.a : Mathf.Max(color.a, MinAlpha);
        if (MultiplyColorByAlpha == MultiplyColorByAlphaType.AfterApplyingMinAlpha)
        {
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        foreach (var spriteRenderer in SpriteRenderers)
        {
            if (SetAlphaOnly)
                spriteRenderer.color = spriteRenderer.color.WithAlpha(color.a * Intensity);
            else
                spriteRenderer.color = color * Intensity;

            if (HideIfAlphaOutOfRange)
                spriteRenderer.enabled = color.a >= HideAlphaRangeMin && color.a <= HideAlphaRangeMax;
        }

        if (SetOnlyOnce) enabled = false;
    }
}
