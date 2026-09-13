using UnityEngine;

public class GlobalShaderColorLightsController : MonoBehaviour, IEnvironmentComponentUpdate
{
    [SerializeField] public LightIntensityData[] LightIntensityData;
    [SerializeField] public bool OverrideSaturation;
    [SerializeField] public float Saturation = 0.5f;

    private static readonly int globalLightTintColorPropertyId = Shader.PropertyToID("_GlobalLightTintColor");

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

    protected virtual bool Initialize() =>
        LightIntensityData != null && LightIntensityData.Length > 0;

    public void Refresh()
    {
        if (!HasInitialized) return;
        var rgbColor = default(Color);
        foreach (var data in LightIntensityData)
        {
            var color = data.Color;
            var num = data.Intensity * color.a;
            rgbColor.r += color.r * num;
            rgbColor.g += color.g * num;
            rgbColor.b += color.b * num;
        }

        rgbColor /= LightIntensityData.Length;
        Color.RGBToHSV(rgbColor, out var h, out var s, out var v);
        v = 1f;
        if (OverrideSaturation) s = Saturation;

        SetColor(Color.HSVToRGB(h, s, v));
    }

    private static void SetColor(Color color) => Shader.SetGlobalColor(globalLightTintColorPropertyId, color);

    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
