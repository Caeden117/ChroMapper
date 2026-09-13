using System.Linq;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorWithLightIdData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientTintColorLightsController>
{
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public int BloomPrePassBackgroundColorsGradient;
    public bool UseGrayscale;
    public float GrayscaleFactor;

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientTintColorLightsController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.BloomPrePassBackgroundColorsGradient = container
            .GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(BloomPrePassBackgroundColorsGradient);
        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;

        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.UseGrayscale = UseGrayscale;
        comp.GrayscaleFactor = GrayscaleFactor;
    }
}
