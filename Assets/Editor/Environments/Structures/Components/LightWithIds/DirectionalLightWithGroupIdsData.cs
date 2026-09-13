using System.Linq;
using UnityEngine;

public class DirectionalLightWithGroupIdsData : EnvironmentComponentData<DirectionalLightsGroupController>
{
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int DirectionalLight;

    public override void FillComponents(
        GameObject self,
        DirectionalLightsGroupController comp,
        CreateContainer container)
    {
        container.LightWithIds.Add(InstanceId, comp);

        comp.enabled = true;
        comp.Light = container.GetComponentOrNull<DirectionalLight>(DirectionalLight);
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
    }
}
