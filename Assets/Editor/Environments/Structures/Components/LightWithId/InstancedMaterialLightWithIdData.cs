using Newtonsoft.Json;
using UnityEngine;

public class InstancedMaterialLightWithIdData : EnvironmentComponentData<InstancedMaterialLightController>
{
    [JsonProperty("lightId")] public int Id;

    [JsonProperty("lightIntensity")] public float Intensity;
    public bool HDR;
    public float MinAlpha;
    public bool SetColorOnly;
    public int MultiplyColorByAlpha;
    public bool SaturateIntensity;

    public override void FillComponents(
        GameObject self,
        InstancedMaterialLightController comp,
        CreateContainer container)
    {
        comp.MpbColorSetter = self.GetOrAddComponent<MaterialPropertyBlockColorSetter>();
        comp.Intensity = Intensity;
        comp.HDR = HDR;
        comp.MinAlpha = MinAlpha;
        comp.SetColorOnly = SetColorOnly;
        comp.MultiplyColorByAlpha = (MultiplyColorByAlphaType)MultiplyColorByAlpha;
        comp.SaturateIntensity = SaturateIntensity;
    }
}
