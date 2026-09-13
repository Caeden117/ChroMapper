using Newtonsoft.Json;
using UnityEngine;

public class RectangleFakeGlowData : EnvironmentComponentData<RectangleFakeGlowLightController>
{
    [JsonProperty("rectangleSize")] public Vector2 Size;
    public float EdgeSize = 0.1f;

    public override void FillComponents(
        GameObject self,
        RectangleFakeGlowLightController comp,
        CreateContainer container)
    {
        comp.Size = Size;
        comp.EdgeSize = EdgeSize;
    }
}
