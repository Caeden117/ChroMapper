using UnityEngine;

public class SpriteLightController : LightController
{
    public SpriteRenderer Renderer;

    public bool HideIfAlphaOutOfRange;
    public float HideAlphaRangeMin = 0.001f;
    public float HideAlphaRangeMax = 1f;
    public float Intensity = 1f;
    public float MinAlpha;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SetColorOnly;
    public bool SetAlphaOnly;
    public bool SetOnlyOnce;

    public override bool IsPhysical => true;
    protected override bool Initialize() => Renderer != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        if (MultiplyColorByAlpha == MultiplyColorByAlphaType.BeforeApplyingMinAlpha)
        {
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        color.a = SetColorOnly ? Renderer.color.a : Mathf.Max(color.a, MinAlpha);
        if (MultiplyColorByAlpha == MultiplyColorByAlphaType.AfterApplyingMinAlpha)
        {
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        if (SetAlphaOnly)
        {
            var col = Renderer.color;
            col.a = color.a * Intensity;
            Renderer.color = col;
        }
        else
            Renderer.color = color * Intensity;

        if (HideIfAlphaOutOfRange) Renderer.enabled = color.a >= HideAlphaRangeMin && color.a <= HideAlphaRangeMax;
        if (SetOnlyOnce) enabled = false;
    }
}
