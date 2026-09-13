using UnityEngine;

public class LightmapIntensityData : LightController
{
    public float Intensity;
    public float ProbeHighlightsIntensityMultiplier = 1f;

    protected override bool Initialize() => true;
    public override void SetColor(Color color) => Color = color;
}
