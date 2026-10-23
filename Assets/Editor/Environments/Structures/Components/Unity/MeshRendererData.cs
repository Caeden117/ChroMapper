using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshRendererData : EnvironmentComponentData<MeshRenderer>
{
    [JsonProperty("materials")] public List<string> Materials = new();

    [JsonProperty("lightmapIndex")] public int LightmapIndex = -1;
    [JsonProperty("lightmapScaleOffset")] public Vector4 LightmapScaleOffset = new(1f, 1f, 0f, 0f);

    public override void FillComponents(GameObject self, MeshRenderer comp, CreateContainer container)
    {
        if (LightmapIndex >= 0)
        {
            var atlas = self.GetOrAddComponent<BakedLightmapAtlas>();
            atlas.Configure(comp, LightmapIndex, LightmapScaleOffset);
        }
    }
}
