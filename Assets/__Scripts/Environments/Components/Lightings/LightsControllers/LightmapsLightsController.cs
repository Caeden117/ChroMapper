using UnityEngine;

public class LightmapsLightsController : MonoBehaviour, IEnvironmentComponentUpdate
{
    [SerializeField] public float MaxTotalIntensity = 1f;
    [SerializeField] public LightmapsIntensityData[] LightIntensityData;

    protected bool HasInitialized;
    protected Color Color;

    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        Color = new Color(0f, 0.5f, 1f);
        Start();
    }

    private void Start() => HasInitialized = Initialize();

    private bool Initialize() => true;

    public void Refresh()
    {
        var totalIntensity = 0f;
        foreach (var data in LightIntensityData) totalIntensity += data.Color.grayscale * data.Weight;

        var intensity = 1f;
        if (totalIntensity > MaxTotalIntensity) intensity = MaxTotalIntensity / totalIntensity;

        intensity = Mathf.LinearToGammaSpace(intensity);
        foreach (var data in LightIntensityData)
        {
            var lightmapColor = data.Color;
            var lightmapIntensity = data.Intensity * intensity * lightmapColor.a;
            lightmapColor.r *= lightmapIntensity;
            lightmapColor.g *= lightmapIntensity;
            lightmapColor.b *= lightmapIntensity;

            var probeColor = data.Color;
            var probeIntensity = Mathf.LinearToGammaSpace(probeColor.a) * intensity;
            probeColor.r *= probeIntensity;
            probeColor.g *= probeIntensity;
            probeColor.b *= probeIntensity;
            probeColor.a *= 2f * intensity;

            data.SetDataToShaders(lightmapColor.linear, probeColor.linear);
        }
    }

    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
