using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class TubeBloomPrePassLightWithIdData : EnvironmentComponentData<ParametricBloomFogLightController>
{
    [JsonProperty("lightId")] public int Id;

    [CanBeNull] public TubeBloomPrePassLightComponent TubeBloomPrePassLight;

    public override void FillComponents(
        GameObject self,
        ParametricBloomFogLightController comp,
        CreateContainer container)
    {
        if (TubeBloomPrePassLight is null) return;

        comp.BloomFog = self.AddComponent<BloomFogObject>();
        comp.BoxLight = container.GetComponentOrNull<ParametricBoxLight>(TubeBloomPrePassLight.ParametricBoxController);
        comp.SpriteLight =
            container.GetComponentOrNull<ParametricSpriteLight>(TubeBloomPrePassLight.Dynamic3SliceSprite);

        TubeBloomPrePassLight.FillComponents(self, comp, container);
    }
}

public class TubeBloomPrePassLightComponent : EnvironmentComponentData<ParametricBloomFogLightController>
{
    public float ColorAlphaMultiplier;
    public float BloomFogIntensityMultiplier;
    public float TubeLength;
    public float TubeWidth;
    public float Center;
    public float StartAlpha;
    public float EndAlpha;
    public float StartWidth;
    public float EndWidth;
    public float BoostToWhite;
    public bool LimitAlpha;
    public float MinAlpha;
    public float MaxAlpha;
    public float LightWidthMultiplier;
    public float MultiplyLengthByAlphaBloomFogMultiplier;
    public bool UseCollision;
    public bool OverrideChildrenLength;
    public float FakeBloomIntensityMultiplier;
    public bool AddWidthToLength;
    public bool ThickenWithDistance;
    public float MinDistance;
    public float MaxDistance;
    public float MinWidthMultiplier;
    public float MaxWidthMultiplier;
    public bool DisableRenderersOnZeroAlpha;
    public float BakedGlowWidthScale;
    public bool MultiplyLengthByAlpha;
    public bool UpdateAlways;
    public bool OverrideChildrenWidth;
    public bool OverrideChildrenAlpha;

    public AnimationCurveData ThickenCurve;
    public AnimationCurveData AlphaToLengthBloomFogCurve;
    public AnimationCurveData AlphaToLengthCurve;

    public int ParametricBoxController;
    public int Dynamic3SliceSprite;

    public override void FillComponents(
        GameObject self,
        ParametricBloomFogLightController comp,
        CreateContainer container)
    {
        comp.ColorAlphaMultiplier = ColorAlphaMultiplier;
        comp.BloomFogIntensityMultiplier = BloomFogIntensityMultiplier;
        comp.Length = TubeLength;
        comp.Width = TubeWidth;
        comp.Center = Center;
        comp.StartAlpha = StartAlpha;
        comp.EndAlpha = EndAlpha;
        comp.StartWidth = StartWidth;
        comp.EndWidth = EndWidth;
        comp.BoostToWhite = BoostToWhite;
        comp.LimitAlpha = LimitAlpha;
        comp.MinAlpha = MinAlpha;
        comp.MaxAlpha = MaxAlpha;
        comp.LightWidthMultiplier = LightWidthMultiplier;
        comp.MultiplyLengthByAlphaBloomFogMultiplier = MultiplyLengthByAlphaBloomFogMultiplier;
        comp.UseCollision = UseCollision;
        comp.OverrideChildrenLength = OverrideChildrenLength;
        comp.FakeBloomIntensityMultiplier = FakeBloomIntensityMultiplier;
        comp.AddWidthToLength = AddWidthToLength;
        comp.ThickenWithDistance = ThickenWithDistance;
        comp.MinDistance = MinDistance;
        comp.MaxDistance = MaxDistance;
        comp.MinWidthMultiplier = MinWidthMultiplier;
        comp.MaxWidthMultiplier = MaxWidthMultiplier;
        comp.DisableRenderersOnZeroAlpha = DisableRenderersOnZeroAlpha;
        comp.BakedGlowWidthScale = BakedGlowWidthScale;
        comp.MultiplyLengthByAlpha = MultiplyLengthByAlpha;
        comp.UpdateAlways = UpdateAlways;
        comp.OverrideChildrenWidth = OverrideChildrenWidth;
        comp.OverrideChildrenAlpha = OverrideChildrenAlpha;

        comp.ThickenCurve = ThickenCurve.Create();
        comp.AlphaToLengthBloomFogCurve = AlphaToLengthBloomFogCurve.Create();
        comp.AlphaToLengthCurve = AlphaToLengthCurve.Create();
    }
}
