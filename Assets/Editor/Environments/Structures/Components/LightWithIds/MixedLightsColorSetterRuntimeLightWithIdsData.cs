using System.Linq;
using UnityEngine;

public class MixedLightsColorSetterRuntimeLightWithIdsData : EnvironmentComponentData<MixedLightsController>
{
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public int MaterialPropertyBlockColorSetter;
    public float LightMultiplier = 1f;

    public override void
        FillComponents(GameObject self, MixedLightsController comp, CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.MpbColorSetter =
            container.GetComponentOrNull<MaterialPropertyBlockColorSetter>(MaterialPropertyBlockColorSetter);
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;

        comp.LightMultiplier = LightMultiplier;
    }
}
