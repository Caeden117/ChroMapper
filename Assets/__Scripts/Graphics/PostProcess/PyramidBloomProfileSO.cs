using UnityEngine;

[CreateAssetMenu(fileName = "PyramidBloomProfile", menuName = "Graphics/Pyramid Bloom Profile")]
public sealed class PyramidBloomProfileSO : ScriptableObject
{
    [SerializeField, Min(0f)] private float radius = 5f;
    [SerializeField, Min(0f)] private float intensity = 1f;
    [SerializeField, Min(0f)] private float pyramidWeightsParam = 0.01f;
    [SerializeField] private float downIntensityOffset = 1f;
    [SerializeField] private float firstUpsampleBrightness = 1f;
    [SerializeField] private float finalUpsampleBrightness = 1f;
    [SerializeField, Min(0f)] private float bloomThreshold = 4f;
    [SerializeField, Min(0f)] private float autoExposureLimit = 1000f;
    [SerializeField] private bool legacyAutoExposure;

    public float Radius => radius;
    public float Intensity => intensity;
    public float PyramidWeightsParam => pyramidWeightsParam;
    public float DownIntensityOffset => downIntensityOffset;
    public float FirstUpsampleBrightness => firstUpsampleBrightness;
    public float FinalUpsampleBrightness => finalUpsampleBrightness;
    public float BloomThreshold => bloomThreshold;
    public float AutoExposureLimit => autoExposureLimit;
    public bool LegacyAutoExposure => legacyAutoExposure;
}
