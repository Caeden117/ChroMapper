using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Imports a mesh renderer's material references and authored visibility.
/// </summary>
public class MeshRendererData : EnvironmentComponentData<MeshRenderer>
{
    [JsonProperty("materials")] public List<string> Materials = new();

    // Preserve Unity's default visibility when the export omits the flag.
    public MeshRendererData() => IsEnabled = true;

    public override void FillComponents(GameObject self, MeshRenderer comp, CreateContainer container)
    {
        comp.enabled = IsEnabled;
    }
}
