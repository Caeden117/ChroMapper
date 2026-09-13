using Newtonsoft.Json;
using UnityEngine;

public class ParticleSystemLightWithIdsData : EnvironmentComponentData<ParticleSystemLightsController>
{
    [JsonProperty("lightId")] public int Id;

    public float Intensity;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public int ParticleSystem;
    public bool SetOnlyOnce;
    public bool SetColorOnly;
    public float MinAlpha;

    public override void FillComponents(
        GameObject self,
        ParticleSystemLightsController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.ParticleSystem = container.GetComponentOrNull<ParticleSystem>(ParticleSystem);
        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;

        comp.SetOnlyOnce = SetOnlyOnce;
        comp.SetColorOnly = SetColorOnly;
        comp.MinAlpha = MinAlpha;
    }
}
