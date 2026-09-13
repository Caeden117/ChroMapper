using System;
using UnityEngine;

public abstract class FireEffect : LightController
{
    // ChroMapper's Lit and particle adapters expose the source effect start time
    // through their shared instanced _StartTime property.
    protected static readonly int EffectStartSongTimeId = Shader.PropertyToID("_StartTime");
    protected static readonly int PrivatePointLightColorId = Shader.PropertyToID("_PrivatePointLightColor");
    protected static readonly int EmissionTexColorId = Shader.PropertyToID("_EmissionTexColor");

    protected static float SongBpmTimeToShaderSeconds(float songBpmTime)
    {
        var container = BeatSaberSongContainer.Instance;
        if (container == null || container.Info == null)
            throw new InvalidOperationException("Fire effect requires a loaded song.");

        var bpm = container.Info.BeatsPerMinute;
        if (!(bpm > 0f))
            throw new InvalidOperationException("Fire effect requires a positive song BPM.");

        return songBpmTime * 60f / bpm;
    }

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

    private bool renderersEnabled;

    protected void SetRenderersEnabled(bool enabled)
    {
        var renderers = FlipBookPropertyBlockController.Renderers;
        for (var i = 0; i < renderers.Count; i++) renderers[i].enabled = enabled;
        BloomPrePassRenderer.enabled = enabled;
    }

    protected void NotifyAlphaWasChanged(float currentAlpha)
    {
        if (ContributeCustomLightColor)
        {
            // activeContainers(
            //     CustomLightColor.WithAlpha(currentAlpha));
        }
    }
}
