using UnityEngine;

public class ContinuousFireEffect : FireEffect
{
    public float FadeInDuration = 1f;
    public float FadeOutDuration = 1f;
    public float SustainDuration = 1f;
    public AnimationCurve FlipbookSustainCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public AnimationCurve BloomSustainCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private float fadeInEndTime;
    private float fadeOutStartTime;
    private float effectStartTime;
    private float effectEndTime;
    private float lastSustainProgress;
    private float lastFadeOutProgress;

    protected override bool Initialize()
    {
        SetInitialValues();
        enabled = false;
        return true;
    }

    public override void SetColor(Color c) { }

    public override void SetColor(Color c, LightColorEventStateData evt, float time)
    {
        if (evt.Brightness <= 0f)
        {
            EndEffect();
            enabled = false;
        }
        else if (!enabled)
        {
            StartEffect(evt.StartTime, evt.EndTime, time);
            enabled = true;
        }
        else
            UpdateEffect(time);
    }

    private void SetInitialValues()
    {
        FlipBookPropertyBlockController.Mpb.SetFloat(EffectStartSongTimeId, -1000f);
        UpdateRenderers(0f, 0f);
        SetRenderersEnabled(enabled: false);
    }

    private void StartEffect(float startTime, float endTime, float songTime)
    {
        effectStartTime = startTime;
        effectEndTime = endTime;
        fadeInEndTime = Mathf.Min(effectStartTime + FadeInDuration, effectEndTime);
        fadeOutStartTime = Mathf.Max(effectEndTime - FadeOutDuration, effectStartTime);
        SetRenderersEnabled(enabled: true);
        FlipBookPropertyBlockController.Mpb.SetFloat(
            EffectStartSongTimeId,
            SongBpmTimeToShaderSeconds(startTime));
        FlipBookPropertyBlockController.ApplyChanges();
        UpdateEffect(songTime);
    }

    private void EndEffect()
    {
        UpdateRenderers(0f, 0f);
        SetRenderersEnabled(enabled: false);
    }

    private void UpdateEffect(float songTime)
    {
        var t = Mathf.InverseLerp(
            effectStartTime,
            fadeInEndTime,
            Mathf.Clamp(songTime, effectStartTime, fadeInEndTime));
        var t2 = 1f
            - Mathf.InverseLerp(
                fadeOutStartTime,
                effectEndTime,
                Mathf.Clamp(songTime, fadeOutStartTime, effectEndTime));
        var time = (songTime - effectStartTime) / SustainDuration;
        var num = FlipbookSustainCurve.Evaluate(time);
        var num2 = BloomSustainCurve.Evaluate(time);
        var num3 = Easing.Quadratic.Out(t) * Easing.Quadratic.InOut(t2);
        UpdateRenderers(num3 * num, num3 * num2);
    }

    private void UpdateRenderers(float flipBookAlpha, float bloomAlpha)
    {
        FlipBookPropertyBlockController.Mpb.SetColor(
            ColorId,
            new Color(1f, 1f, 1f, flipBookAlpha));
        FlipBookPropertyBlockController.ApplyChanges();
        BloomPropertyBlockController.Mpb.SetColor(
            ColorId,
            new Color(1f, 1f, 1f, bloomAlpha * BloomIntensityMultiplier));
        BloomPropertyBlockController.ApplyChanges();
        NotifyAlphaWasChanged(bloomAlpha);
        if (!UseEmissionColor)
        {
            PrivatePointLightPropertyBlockController.Mpb.SetColor(
                PrivatePointLightColorId,
                PointLightColor * bloomAlpha);
            PrivatePointLightPropertyBlockController.ApplyChanges();
        }
        else
        {
            EmissionTextureColorPropertyBlockController.Mpb.SetColor(
                EmissionTexColorId,
                PointLightColor * bloomAlpha);
            EmissionTextureColorPropertyBlockController.ApplyChanges();
        }
    }
}
