using System;
using Newtonsoft.Json;
using UnityEngine;

public abstract class FireEffectData<T> : EnvironmentComponentData<T> where T : FireEffect
{
    [JsonProperty("lightId")] public int Id;

    public int FlipBookPropertyBlockController;
    public int BloomPropertyBlockController;
    public bool UseEmissionColor;

    public int PrivatePointLightPropertyBlockController;
    public int EmissionTextureColorPropertyBlockController;
    public int BloomPrePassRenderer;

    public float BloomIntensityMultiplier = 1f;
    public Color PointLightColor = Color.yellow;

    public bool ContributeCustomLightColor = true;
    public Color Color = Color.white;

    protected void FillFireEffect(T comp, CreateContainer container)
    {
        comp.FlipBookPropertyBlockController = Require<MaterialPropertyBlockController>(
            container,
            FlipBookPropertyBlockController,
            nameof(FlipBookPropertyBlockController));
        comp.BloomPropertyBlockController = Require<MaterialPropertyBlockController>(
            container,
            BloomPropertyBlockController,
            nameof(BloomPropertyBlockController));
        comp.BloomPrePassRenderer = Require<BloomPrePassBackgroundNonLightRenderer>(
            container,
            BloomPrePassRenderer,
            nameof(BloomPrePassRenderer));
        comp.UseEmissionColor = UseEmissionColor;
        comp.ContributeCustomLightColor = ContributeCustomLightColor;
        comp.BloomIntensityMultiplier = BloomIntensityMultiplier;
        comp.PointLightColor = PointLightColor;
        comp.CustomLightColor = Color;

        if (UseEmissionColor)
            comp.EmissionTextureColorPropertyBlockController = Require<MaterialPropertyBlockController>(
                container,
                EmissionTextureColorPropertyBlockController,
                nameof(EmissionTextureColorPropertyBlockController));
        else
            comp.PrivatePointLightPropertyBlockController = Require<MaterialPropertyBlockController>(
                container,
                PrivatePointLightPropertyBlockController,
                nameof(PrivatePointLightPropertyBlockController));
    }

    private static TComponent Require<TComponent>(CreateContainer container, int instanceId, string field)
        where TComponent : Component
    {
        var component = container.GetComponentOrNull<TComponent>(instanceId);
        if (component == null)
            throw new InvalidOperationException(
                $"Fire effect requires {field} reference {instanceId} ({typeof(TComponent).Name}).");
        return component;
    }
}

public class BurstFireEffectData : FireEffectData<BurstFireEffect>
{
    public float FadeOutDuration = 1f;
    public AnimationCurveData FlipbookFadeOutCurve;
    public AnimationCurveData BloomFadeOutCurve;

    public override void FillComponents(GameObject self, BurstFireEffect comp, CreateContainer container)
    {
        if (FlipbookFadeOutCurve == null || BloomFadeOutCurve == null)
            throw new InvalidOperationException("Burst fire effect requires both fade-out curves.");
        if (FadeOutDuration <= 0f)
            throw new InvalidOperationException("Burst fire fade-out duration must be positive.");

        comp.FadeOutDuration = FadeOutDuration;
        comp.FlipbookFadeOutCurve = FlipbookFadeOutCurve.Create();
        comp.BloomFadeOutCurve = BloomFadeOutCurve.Create();
        FillFireEffect(comp, container);
    }
}

public class ContinuousFireEffectData : FireEffectData<ContinuousFireEffect>
{
    public float FadeInDuration = 1f;
    public float FadeOutDuration = 1f;
    public float SustainDuration = 1f;
    public AnimationCurveData FlipbookSustainCurve;
    public AnimationCurveData BloomSustainCurve;

    public override void FillComponents(GameObject self, ContinuousFireEffect comp, CreateContainer container)
    {
        if (FlipbookSustainCurve == null || BloomSustainCurve == null)
            throw new InvalidOperationException("Continuous fire effect requires both sustain curves.");
        if (FadeInDuration < 0f || FadeOutDuration < 0f || SustainDuration <= 0f)
            throw new InvalidOperationException(
                "Continuous fire durations must be non-negative and sustain must be positive.");

        comp.FadeInDuration = FadeInDuration;
        comp.FadeOutDuration = FadeOutDuration;
        comp.SustainDuration = SustainDuration;
        comp.FlipbookSustainCurve = FlipbookSustainCurve.Create();
        comp.BloomSustainCurve = BloomSustainCurve.Create();
        FillFireEffect(comp, container);
    }
}
