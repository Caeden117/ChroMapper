using Newtonsoft.Json;
using UnityEngine;

public class ParticleSystemLightWithIdData : EnvironmentComponentData<ParticleSystemLightController>
{
    [JsonProperty("lightId")] public int Id;

    public int ParticleSystem;

    public bool SetOnlyOnce;
    public bool SetColorOnly;
    [JsonProperty("lightIntensity")] public float Intensity = 1f;
    public float MinAlpha;


    public override void FillComponents(
        GameObject self,
        ParticleSystemLightController comp,
        CreateContainer container)
    {
        comp.ParticleSystem = ParticleSystem == 0
            ? self.GetComponent<ParticleSystem>()
            : container.GetComponentOrNull<ParticleSystem>(ParticleSystem);
        comp.SetOnlyOnce = SetOnlyOnce;
        comp.SetColorOnly = SetColorOnly;
        comp.Intensity = Intensity;
        comp.MinAlpha = MinAlpha;
    }
}
