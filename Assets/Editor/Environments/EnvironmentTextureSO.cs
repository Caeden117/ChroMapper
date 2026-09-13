using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Texture", fileName = "EnvironmentTextureSO")]
public class EnvironmentTextureSO : ScriptableObject
{
    [SerializeField] public List<TextureInfo> list = new();

    public readonly Dictionary<string, Texture> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    /// <summary>Rebuilds the runtime hash lookup from the serialized texture entries.</summary>
    public void RebuildLookup() => Initialize();

    public void Initialize()
    {
        Lookup.Clear();
        if (list == null) return;

        foreach (var entry in list)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Hash)) continue;
            Lookup[entry.Hash] = entry.Texture;
        }
    }

    public void MarkForChange()
    {
        list.ForEach(x =>
        {
            x.Unused = true;
            x.Environments.Clear();
        });
    }

    public void RemoveUnused() => list.RemoveAll(x => x.Unused);

    public void AddEntry(string textureHash, string textureName, string environment)
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Hash != textureHash) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Hash != textureHash))
        {
            list.Add(
                new TextureInfo
                {
                    Hash = textureHash, Name = textureName, Environments = new List<string> { environment }
                });
        }
        else
        {
            var m = list.First(x => x.Hash == textureHash);
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    public void Sort() => list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
}

[Serializable]
public class TextureInfo
{
    public string Name;
    public string Hash;
    public Texture Texture;

    public List<string> Environments;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
