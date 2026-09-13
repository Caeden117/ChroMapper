using Newtonsoft.Json;
using UnityEngine;

public class DirectionalLightWithIdData : EnvironmentComponentData<DirectionalLightController>
{
    [JsonProperty("lightId")] public int Id;

    public float Intensity;
    public float MinIntensity;
    public string Light;

    public override void FillComponents(
        GameObject self,
        DirectionalLightController comp,
        CreateContainer container)
    {
        comp.Light = container.ChromaIdObjects[Light].GetComponent<DirectionalLight>();
        comp.Intensity = Intensity;
        comp.MinIntensity = MinIntensity;
    }
}
