using Newtonsoft.Json;
using UnityEngine;

public class LightIntensityIdData
{
    [JsonProperty("lightId")] public int ID;
    public float Intensity;

    public void CopyTo(LightIntensityData comp) => comp.Intensity = Intensity;
}
