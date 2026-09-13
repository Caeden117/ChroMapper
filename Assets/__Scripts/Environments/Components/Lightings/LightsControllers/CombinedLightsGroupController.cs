using System;
using UnityEngine;

public abstract class CombinedLightsGroupController : MonoBehaviour, IEnvironmentComponentUpdate
{
    public LightIntensityData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;

    protected bool HasInitialized;
    protected Color Color;

    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        Color = new Color(0f, 0.5f, 1f);
        Start();
    }

    private void Start()
    {
        HasInitialized = Initialize();
        SetColor(Color);
    }

    protected abstract bool Initialize();

    public void Refresh()
    {
        Color color = default;
        for (var i = 0; i < LightIntensityData.Length; i++)
        {
            var lightIntensitiesWithId = LightIntensityData[i];
            var color2 = ProcessColor(lightIntensitiesWithId.Color, lightIntensitiesWithId.Intensity);
            if (color.r < color2.r) color.r = color2.r;
            if (color.g < color2.g) color.g = color2.g;
            if (color.b < color2.b) color.b = color2.b;
            if (color.a < color2.a) color.a = color2.a;
        }

        if (MultiplyColorByAlpha)
        {
            color *= Intensity;
            var grayscale = color.grayscale;
            if (grayscale > MaxIntensity) color /= grayscale / MaxIntensity;
        }
        else
        {
            color.a *= Intensity;
            color.a = Mathf.Min(MaxIntensity, color.a);
        }

        SetColor(color);
    }

    private Color ProcessColor(Color color, float intensity)
    {
        color.a *= intensity;
        if (MultiplyColorByAlpha)
        {
            color.a = Mathf.Sqrt(color.a);
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        return color;
    }

    public abstract void SetColor(Color color);
    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
