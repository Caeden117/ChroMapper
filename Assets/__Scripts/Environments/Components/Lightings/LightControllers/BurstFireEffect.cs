using UnityEngine;

public class BurstFireEffect : FireEffect
{
    public float FadeOutDuration = 1f;
    public AnimationCurve FlipbookFadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public AnimationCurve BloomFadeOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

    protected override bool Initialize()
    {
        SetInitialValues();
        return true;
    }

    public override void SetColor(Color color) { }

    public override void SetColor(Color color, LightColorEventStateData evt, float time)
    {
        if (time - evt.StartTime > FadeOutDuration || time < evt.StartTime)
            EndEffect();
        else if (!(evt.Brightness <= 0f))
            StartEffect(evt.StartTime);
        else
        {
            UpdateFadeOutProgress(
                Mathf.InverseLerp(evt.StartTime, evt.StartTime + FadeOutDuration, time - evt.StartTime));
        }
    }

    private void StartEffect(float time)
    {
        SetRenderersEnabled(enabled: true);
        FlipBookPropertyBlockController.Mpb.SetFloat(
            EffectStartSongTimeId,
            SongBpmTimeToShaderSeconds(time));
        FlipBookPropertyBlockController.ApplyChanges();
        UpdateFadeOutProgress(0f);
    }

    private void EndEffect()
    {
        UpdateFadeOutProgress(1f);
        SetRenderersEnabled(enabled: false);
        NotifyAlphaWasChanged(0f);
    }

    private void SetInitialValues()
    {
        if (FlipBookPropertyBlockController != null)
            FlipBookPropertyBlockController.Mpb.SetFloat(
                EffectStartSongTimeId,
                -1000f);
        UpdateFadeOutProgress(1f);
        SetRenderersEnabled(false);
        NotifyAlphaWasChanged(0f);
    }

    private void UpdateFadeOutProgress(float fadeOutProgress)
    {
        FlipBookPropertyBlockController.Mpb.SetColor(
            ColorId,
            new Color(1f, 1f, 1f, FlipbookFadeOutCurve.Evaluate(fadeOutProgress)));
        FlipBookPropertyBlockController.ApplyChanges();
        var materialPropertyBlock = BloomPropertyBlockController.Mpb;
        var bloomValue = BloomFadeOutCurve.Evaluate(fadeOutProgress);
        materialPropertyBlock.SetColor(
            ColorId,
            new Color(1f, 1f, 1f, bloomValue * BloomIntensityMultiplier));
        BloomPropertyBlockController.ApplyChanges();
        NotifyAlphaWasChanged(bloomValue);
        if (!UseEmissionColor)
        {
            PrivatePointLightPropertyBlockController.Mpb.SetColor(
                PrivatePointLightColorId,
                PointLightColor * bloomValue);
            PrivatePointLightPropertyBlockController.ApplyChanges();
        }
        else
        {
            EmissionTextureColorPropertyBlockController.Mpb.SetColor(
                EmissionTexColorId,
                PointLightColor * bloomValue);
            EmissionTextureColorPropertyBlockController.ApplyChanges();
        }
    }
}
