using System;
using UnityEngine;

public abstract class CombinedLightsController : MonoBehaviour, IEnvironmentComponentUpdate
{
    public LightIntensityData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public ColorMixAndWeightingApproach MixType;

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
            var lightIntensityData = LightIntensityData[i];
            var processed = ProcessColor(lightIntensityData.Color, lightIntensityData.Intensity);
            switch (MixType)
            {
                case ColorMixAndWeightingApproach.Maximum:
                    if (color.r < processed.r) color.r = processed.r;
                    if (color.g < processed.g) color.g = processed.g;
                    if (color.b < processed.b) color.b = processed.b;
                    if (color.a < processed.a) color.a = processed.a;
                    break;
                case ColorMixAndWeightingApproach.FractionAndSum:
                    color.r += processed.r;
                    color.g += processed.g;
                    color.b += processed.b;
                    break;
            }
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

        SetColor(Color = color);
    }

    private Color ProcessColor(Color color, float intensity)
    {
        switch (MixType)
        {
            case ColorMixAndWeightingApproach.Maximum:
                color.a *= intensity;
                color.a = Mathf.Sqrt(color.a);
                break;
            case ColorMixAndWeightingApproach.FractionAndSum:
                color.a *= intensity;
                break;
        }

        if (!MultiplyColorByAlpha) return color;
        color.r *= color.a;
        color.g *= color.a;
        color.b *= color.a;

        return color;
    }

    public abstract void SetColor(Color color);

    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
