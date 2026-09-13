using System;
using UnityEngine;

public class LightIntensityData : LightController
{
    public float Intensity;

    protected override bool Initialize() => true;
    public override void SetColor(Color color) => Color = color;
}
