using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshRendererData : EnvironmentComponentData<MeshRenderer>
{
    [JsonProperty("materials")] public List<string> Materials = new();

    public override void FillComponents(GameObject self, MeshRenderer comp, CreateContainer container)
    {
    }
}
