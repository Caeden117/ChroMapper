using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorLightsController : CombinedLightsController
{
    public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;
    public bool UseGrayscale;
    public float GrayscaleFactor;

    protected override bool Initialize() => BloomPrePassBackgroundColorsGradient != null;

    public override void SetColor(Color color)
    {
        if (!HasInitialized) return;
        if (UseGrayscale)
            color = Color.Lerp(color, Color.white * color.maxColorComponent, Mathf.Clamp01(GrayscaleFactor));
        BloomPrePassBackgroundColorsGradient.TintColor = Color = color;
    }
}
