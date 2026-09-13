using System.Linq;
using UnityEngine;

public class GlobalShaderColorLightWithIdsData : EnvironmentComponentData<GlobalShaderColorLightsController>
{
    public LightIntensityIdData[] LightIntensityData;
    public bool OverrideSaturation;
    public float Saturation = 0.5f;

    public override void FillComponents(
        GameObject self,
        GlobalShaderColorLightsController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.OverrideSaturation = OverrideSaturation;
        comp.Saturation = Saturation;
    }
}
