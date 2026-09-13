using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Sprite", fileName = "EnvironmentSpriteSO")]
public class EnvironmentSpriteSO : ScriptableObject
{
    [SerializeField] public List<SpriteInfo> list = new();

    public readonly Dictionary<string, Sprite> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    // Refresh callers need the runtime lookup immediately, without waiting for Unity to re-enable the asset.
    public void RebuildLookup() => Initialize();

    public void Initialize()
    {
        Lookup.Clear();
        foreach (var entry in list) Lookup[entry.Name] = entry.Sprite;
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

    public void AddEntry(string textureName, string environment)
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Name != textureName) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Name != textureName))
        {
            list.Add(
                new SpriteInfo { Name = textureName, Environments = new List<string> { environment } });
        }
        else
        {
            var m = list.First(x => x.Name == textureName);
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    public void Sort() => list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

    public Sprite GetSafe(string name) => name == "null" ? null : Lookup.GetValueOrDefault(name);
}

[Serializable]
public class SpriteInfo
{
    public string Name;
    public Sprite Sprite;

    public List<string> Environments;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
