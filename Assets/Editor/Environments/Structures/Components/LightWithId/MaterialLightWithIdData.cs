using Newtonsoft.Json;
using UnityEngine;

public class MaterialLightWithIdData : EnvironmentComponentData<MaterialLightController>
{
    [JsonProperty("lightId")] public int Id;

    public float AlphaIntensity;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public bool MultiplyColorWithAlpha;
    public bool MultiplyColor;
    public float ColorMultiplier;
    public float Alpha;
    public string ColorProperty;

    public override void
        FillComponents(GameObject self, MaterialLightController comp, CreateContainer container)
    {
        comp.Renderer = self.GetComponent<Renderer>();
        comp.AlphaIntensity = AlphaIntensity;
        comp.AlphaIntoColor = AlphaIntoColor;
        comp.SetColorOnly = SetColorOnly;
        comp.MultiplyColorWithAlpha = MultiplyColorWithAlpha;
        comp.MultiplyColor = MultiplyColor;
        comp.ColorMultiplier = ColorMultiplier;
        comp.Alpha = Alpha;
        comp.Property = string.IsNullOrEmpty(ColorProperty) ? "_Color" : ColorProperty;
    }
}
