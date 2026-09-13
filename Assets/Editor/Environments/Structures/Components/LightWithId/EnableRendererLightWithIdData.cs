using Newtonsoft.Json;
using UnityEngine;

public class EnableRendererLightWithIdData : EnvironmentComponentData<EnableRendererLightController>
{
    [JsonProperty("lightId")] public int Id;

    public int Renderer;
    public float HideAlphaRangeMin = 0.001f;
    public float HideAlphaRangeMax = 1f;

    public override void
        FillComponents(GameObject self, EnableRendererLightController comp, CreateContainer container)
    {
        comp.Renderer = container.GetComponentOrNull<Renderer>(Renderer);
        comp.HideAlphaRangeMin = HideAlphaRangeMin;
        comp.HideAlphaRangeMax = HideAlphaRangeMax;
    }
}
