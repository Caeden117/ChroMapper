using UnityEngine;

public class DirectionalLightController : LightController
{
    public DirectionalLight Light;

    public float Intensity = 1f;
    public float MinIntensity;

    protected override bool Initialize() => Light != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;
        Light.Intensity = Mathf.Max(color.a * Intensity, MinIntensity);
        Light.Color = color;
    }
}
