using Newtonsoft.Json;
using UnityEngine;

public class SpriteLightWithIdData : EnvironmentComponentData<SpriteLightController>
{
    [JsonProperty("lightId")] public int Id;

    public int SpriteRenderer;

    public bool HideIfAlphaOutOfRange;
    public float HideAlphaRangeMin;
    public float HideAlphaRangeMax;
    [JsonProperty("lightIntensity")] public float Intensity;
    public float MinAlpha;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SetColorOnly;
    public bool SetAlphaOnly;
    public bool SetOnlyOnce;

    public override void
        FillComponents(GameObject self, SpriteLightController comp, CreateContainer container)
    {
        comp.Renderer = container.GetComponentOrNull<SpriteRenderer>(SpriteRenderer);
        comp.HideIfAlphaOutOfRange = HideIfAlphaOutOfRange;
        comp.HideAlphaRangeMin = HideAlphaRangeMin;
        comp.HideAlphaRangeMax = HideAlphaRangeMax;
        comp.Intensity = Intensity;
        comp.MinAlpha = MinAlpha;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.SetColorOnly = SetColorOnly;
        comp.SetAlphaOnly = SetAlphaOnly;
        comp.SetOnlyOnce = SetOnlyOnce;
    }
}
