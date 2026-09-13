using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Library class for on-the-fly asset instantiation/replacement when creating environments from data.
/// If an object name matches one in the library, it will be replaced with the corresponding asset.
/// This handles entire prefabs, as well as shared instances like materials and meshes.
/// </summary>
[CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "Environment/Environment Library")]
public class EnvironmentLibrarySO : ScriptableObject
{
    [SerializeField] public EnvironmentMeshSO Meshes;
    [SerializeField] public EnvironmentMaterialSO Materials;
    [SerializeField] public EnvironmentTextureSO Textures;
    [SerializeField] public EnvironmentSpriteSO Sprites;

    [SerializeField] public List<ShaderEntry> Shaders;
    public readonly Dictionary<string, Shader> ShaderLookup = new();
    [SerializeField] public List<ComputeShaderEntry> ComputeShaders;
    public readonly Dictionary<string, ComputeShader> ComputeShaderLookup = new();
    [SerializeField] public List<LayerMaskEntry> layerMaskRemap = new();
    public readonly Dictionary<string, LayerMask> LayerMaskLookup = new();

    // Special material to use for the skybox
    // Ideally this should be the bloomfog skybox material.
    [field: SerializeField] public Material SkyboxMaterial { get; private set; }
    [field: SerializeField] public Mesh SliceSprite { get; private set; }
    [field: SerializeField] public MirrorRendererSO MirrorRenderer { get; private set; }
    [field: SerializeField] public ParticleSystemEventController ParticleSystemEventControllerPrefab { get; private set; }

    // The fallback prefab to use when no replacement is found
    [SerializeField] public GameObject fallbackPrefab;

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    public void Initialize()
    {
        LayerMaskLookup.Clear();
        foreach (var entry in layerMaskRemap) LayerMaskLookup.Add(entry.name, entry.layerMask);

        ShaderLookup.Clear();
        var nullShaders = new System.Text.StringBuilder();
        foreach (var entry in Shaders)
        {
            ShaderLookup.Add(entry.name, entry.shader);
            if (entry.shader == null)
                nullShaders.Append($"\n  - '{entry.name}'");
        }
        if (nullShaders.Length > 0)
            Debug.LogWarning($"[EnvironmentLibrary] {ShaderLookup.Count(x => x.Value == null)}/{Shaders.Count} Shader entries have no mapped Shader asset. Materials using these will fall back to ChroMapper/Missing (purple).{nullShaders}\nOpen EnvironmentLibrarySO in the Inspector and assign a ChroMapper shader to each entry.");

        ComputeShaderLookup.Clear();
        foreach (var entry in ComputeShaders) ComputeShaderLookup.Add(entry.name, entry.computeShader);

        Meshes.Initialize();
        Materials.Initialize();
        Textures.Initialize();
        Sprites.Initialize();
    }

    public void MarkForChange()
    {
        Meshes.MarkForChange();
        Materials.MarkForChange();
        Textures.MarkForChange();
        Sprites.MarkForChange();
        foreach (var s in Shaders) s.keywords.Clear();
    }

    public void RemoveUnused()
    {
        Meshes.RemoveUnused();
        Materials.RemoveUnused();
        Textures.RemoveUnused();
        Sprites.RemoveUnused();
    }

    public void Sort()
    {
        Meshes.Sort();
        Materials.Sort();
        Textures.Sort();
        Sprites.Sort();
        foreach (var s in Shaders)
            s.keywords.Sort((a, b) => string.Compare(a.Replace("_", ""), b.Replace("_", ""), StringComparison.Ordinal));
        Shaders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        layerMaskRemap = LayerMaskLookup
            .Select(x => new LayerMaskEntry { name = x.Key, layerMask = x.Value })
            .OrderBy(x => x.name)
            .ToList();
    }
}

[Serializable]
public struct LayerMaskEntry
{
    public string name;
    public LayerMask layerMask;
}

[Serializable]
public class ShaderEntry
{
    public string name;
    public Shader shader;
    public List<string> keywords = new();
}

[Serializable]
public class ComputeShaderEntry
{
    public string name;
    public ComputeShader computeShader;
}
