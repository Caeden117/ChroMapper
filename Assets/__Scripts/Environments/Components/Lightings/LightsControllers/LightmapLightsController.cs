using System;
using UnityEngine;

public class LightmapLightsController : MonoBehaviour, IEnvironmentComponentUpdate
{
    [SerializeField] public LightConstants.BakeId BakeId;
    [SerializeField] public float Intensity = 1f;
    [SerializeField] public float ProbeIntensity = 1f;
    [SerializeField] public LightmapIntensityData[] LightIntensityData = Array.Empty<LightmapIntensityData>();
    [SerializeField] public ColorMixAndWeightingApproach MixType;
    [SerializeField] public float NormalizerWeight = 1f;

    private BakedLightsNormalizer bakedLightsNormalizer;
    private int lightmapLightIdColorPropertyId;
    private int lightProbeLightIdColorPropertyId;
    private bool isNormalizerInScene;
    public Color CalculatedColorPreNormalization;

    protected bool HasInitialized;
    protected Color Color;

    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        Color = new Color(0f, 0.5f, 1f);
        Start();
        SetChannelColorDirect(Color);
    }

    private void Start()
    {
        HasInitialized = Initialize();
        Refresh();
    }

    private bool Initialize()
    {
        SetShaderProperties();
        GetBakedLightsNormalizer();
        SetDataToShaders(Color.clear, Color.clear);

        return true;
    }

    public void Refresh()
    {
        if (!HasInitialized) return;

        Color lightmapColor = default;
        Color probeColor = default;
        var normal = isNormalizerInScene ? bakedLightsNormalizer.GetNormalizationMultiplier() : 1f;

        foreach (var data in LightIntensityData)
        {
            var intensity = data.Intensity;
            var lightmapIntensity = data.Color;
            var probeIntensity = lightmapIntensity;

            var lightmapMul = intensity * lightmapIntensity.a;
            lightmapIntensity.r *= lightmapMul;
            lightmapIntensity.g *= lightmapMul;
            lightmapIntensity.b *= lightmapMul;

            var probeMul = Mathf.LinearToGammaSpace(probeIntensity.a) * intensity;
            probeIntensity.r *= probeMul;
            probeIntensity.g *= probeMul;
            probeIntensity.b *= probeMul;
            probeIntensity.a *= 2f * intensity * data.ProbeHighlightsIntensityMultiplier;

            switch (MixType)
            {
                case ColorMixAndWeightingApproach.Maximum:
                    if (lightmapColor.r < lightmapIntensity.r) lightmapColor.r = lightmapIntensity.r;
                    if (lightmapColor.g < lightmapIntensity.g) lightmapColor.g = lightmapIntensity.g;
                    if (lightmapColor.b < lightmapIntensity.b) lightmapColor.b = lightmapIntensity.b;

                    if (probeColor.r < probeIntensity.r) probeColor.r = probeIntensity.r;
                    if (probeColor.g < probeIntensity.g) probeColor.g = probeIntensity.g;
                    if (probeColor.b < probeIntensity.b) probeColor.b = probeIntensity.b;
                    if (probeColor.a < probeIntensity.a) probeColor.a = probeIntensity.a;

                    break;
                case ColorMixAndWeightingApproach.FractionAndSum:
                    lightmapColor.r += lightmapIntensity.r;
                    lightmapColor.g += lightmapIntensity.g;
                    lightmapColor.b += lightmapIntensity.b;
                    lightmapColor.a += lightmapIntensity.a;

                    probeColor.r += probeIntensity.r;
                    probeColor.g += probeIntensity.g;
                    probeColor.b += probeIntensity.b;
                    probeColor.a += probeIntensity.a;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        lightmapColor *= Intensity;
        probeColor *= ProbeIntensity;
        CalculatedColorPreNormalization = probeColor.linear;
        SetDataToShaders(lightmapColor.linear, normal * probeColor.linear);
    }

    public void SetChannelColorDirect(Color channelColor) =>
        Shader.SetGlobalColor(lightProbeLightIdColorPropertyId, channelColor);

    private void SetDataToShaders(Color lightmapColor, Color probeColor)
    {
        Shader.SetGlobalColor(lightmapLightIdColorPropertyId, lightmapColor);
        Shader.SetGlobalColor(lightProbeLightIdColorPropertyId, probeColor);
    }

    private void SetShaderProperties()
    {
        lightmapLightIdColorPropertyId = LightConstants.GetLightmapLightBakeIdPropertyId(BakeId);
        lightProbeLightIdColorPropertyId = LightConstants.GetLightProbeLightBakeIdPropertyId(BakeId);
    }

    private void GetBakedLightsNormalizer()
    {
        bakedLightsNormalizer = FindFirstObjectByType<BakedLightsNormalizer>();
        isNormalizerInScene = bakedLightsNormalizer != null;
    }

    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
