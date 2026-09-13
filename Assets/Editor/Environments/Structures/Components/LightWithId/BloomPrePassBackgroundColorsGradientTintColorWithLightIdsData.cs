using Newtonsoft.Json;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorWithLightIdsData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientTintColorLightController>
{
    [JsonProperty("lightId")] public int Id;

    public int BloomPrePassBackgroundColorsGradient;

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientTintColorLightController comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient =
            container.GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(BloomPrePassBackgroundColorsGradient);
    }
}
