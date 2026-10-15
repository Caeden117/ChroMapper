using System;
using UnityEngine;

public class InstancedMaterialLightController : LightController
{
    public MaterialPropertyBlockColorSetter MpbColorSetter;

    public float Intensity;
    public bool HDR;
    public float MinAlpha;
    public bool SetColorOnly;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SaturateIntensity;

    private bool startColorWasSet;
    private float startAlpha;

    public override bool IsPhysical => true;
    protected override bool Initialize() => MpbColorSetter != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        // Native color-only lights keep the first dispatched alpha while later events recolor them.
        if (!startColorWasSet)
        {
            startAlpha = color.a;
            startColorWasSet = true;
        }

        var a = color.a;
        if (SetColorOnly)
            a = startAlpha;
        else
        {
            a = Mathf.Max(MinAlpha, a) * Intensity;
            if (SaturateIntensity) a = Mathf.Clamp01(a);
        }

        switch (MultiplyColorByAlpha)
        {
            case MultiplyColorByAlphaType.BeforeApplyingMinAlpha:
                color.r = color.a * color.r;
                color.g = color.a * color.g;
                color.b = color.a * color.b;
                break;
            case MultiplyColorByAlphaType.AfterApplyingMinAlpha:
                color.r = a * color.r;
                color.g = a * color.g;
                color.b = a * color.b;
                break;
            case MultiplyColorByAlphaType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        color.a = a;

        if (HDR) color *= Intensity;

        MpbColorSetter.SetColor(color);
    }
}
