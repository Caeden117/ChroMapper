using System;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorLightController : LightController
{
    [SerializeField] public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;

    protected override bool Initialize() => BloomPrePassBackgroundColorsGradient != null;

    public override void SetColor(Color color)
    {
        if (HasInitialized) BloomPrePassBackgroundColorsGradient.TintColor = color;
    }
}
