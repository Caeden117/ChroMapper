using UnityEngine;

public abstract class FireEffect : MonoBehaviour
{
    protected static readonly int ColorId = Shader.PropertyToID("_Color");
    // ChroMapper's Lit and particle adapters expose the source effect start time
    // through their shared instanced _StartTime property.
    protected static readonly int EffectStartSongTimeId = Shader.PropertyToID("_StartTime");
    protected static readonly int PrivatePointLightColorId = Shader.PropertyToID("_PrivatePointLightColor");
    protected static readonly int EmissionTexColorId = Shader.PropertyToID("_EmissionTexColor");

    public MaterialPropertyBlockController FlipBookPropertyBlockController;
    public MaterialPropertyBlockController BloomPropertyBlockController;
    public bool UseEmissionColor;

    public MaterialPropertyBlockController PrivatePointLightPropertyBlockController;
    public MaterialPropertyBlockController EmissionTextureColorPropertyBlockController;
    public BloomPrePassBackgroundNonLightRenderer BloomPrePassRenderer;

    public float BloomIntensityMultiplier = 1f;
    public Color PointLightColor = Color.yellow;

    public bool ContributeCustomLightColor = true;
    public Color CustomLightColor = Color.white;

    public Color ContributionColor { get; private set; } = Color.clear;

    private bool hasInitialized;

    private void Start() => EnsureInitialized();

    internal void EnsureInitialized()
    {
        if (hasInitialized) return;
        Initialize();
        hasInitialized = true;
    }

    protected virtual void Initialize()
    {
        var (flipbookAlpha, bloomAlpha) = InitialAlphas;
        FlipBookPropertyBlockController.Mpb.SetFloat(EffectStartSongTimeId, -1000f);
        UpdateRenderers(flipbookAlpha, bloomAlpha);
        ApplyPointLightColor(PointLightColor * bloomAlpha);
        ContributionColor = CustomLightColor.WithAlpha(0f);
        SetRenderersEnabled(false);
    }

    internal virtual (float flipbookAlpha, float bloomAlpha) InitialAlphas => (0f, 0f);

    internal void Apply(float startSongTime, bool visible, float flipbookAlpha, float bloomAlpha,
        float contributionAlpha)
    {
        EnsureInitialized();
        FlipBookPropertyBlockController.Mpb.SetFloat(EffectStartSongTimeId, startSongTime);
        SetRenderersEnabled(visible);
        UpdateRenderers(flipbookAlpha, bloomAlpha);
        ContributionColor = CustomLightColor.WithAlpha(contributionAlpha);
    }

    protected void SetRenderersEnabled(bool enabled)
    {
        var renderers = FlipBookPropertyBlockController.Renderers;
        for (var i = 0; i < renderers.Count; i++) renderers[i].enabled = enabled;
        BloomPrePassRenderer.enabled = enabled;
    }

    protected void UpdateRenderers(float flipbookAlpha, float bloomAlpha)
    {
        FlipBookPropertyBlockController.Mpb.SetColor(ColorId, new Color(1f, 1f, 1f, flipbookAlpha));
        FlipBookPropertyBlockController.ApplyChanges();
        BloomPropertyBlockController.Mpb.SetColor(
            ColorId,
            new Color(1f, 1f, 1f, bloomAlpha * BloomIntensityMultiplier));
        BloomPropertyBlockController.ApplyChanges();
    }

    internal void ApplyPointLightColor(Color color)
    {
        if (UseEmissionColor)
        {
            EmissionTextureColorPropertyBlockController.Mpb.SetColor(EmissionTexColorId, color);
            EmissionTextureColorPropertyBlockController.ApplyChanges();
        }
        else
        {
            PrivatePointLightPropertyBlockController.Mpb.SetColor(PrivatePointLightColorId, color);
            PrivatePointLightPropertyBlockController.ApplyChanges();
        }
    }
}
