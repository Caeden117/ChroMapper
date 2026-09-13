using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Mesh", fileName = "EnvironmentMeshSO")]
public class EnvironmentMeshSO : ScriptableObject
{
    [SerializeField] public List<MeshInfo> list = new();

    public readonly Dictionary<string, Mesh> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    // Refresh callers need the runtime lookup immediately, without waiting for Unity to re-enable the asset.
    public void RebuildLookup() => Initialize();

    public void Initialize()
    {
        Lookup.Clear();
        foreach (var entry in list) Lookup[entry.Hash] = entry.Mesh;
    }

    public void MarkForChange()
    {
        list.ForEach(x =>
        {
            x.Unused = true;
            x.Environments.Clear();
            x.Names.Clear();
        });
    }

    public void RemoveUnused() => list.RemoveAll(x => x.Unused);

    public void AddEntry(EnvironmentInfoMesh mesh, string environment)
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Hash != mesh.Hash) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Hash != mesh.Hash))
        {
            list.Add(
                new MeshInfo
                {
                    Hash = mesh.Hash,
                    Names = new List<string> { mesh.Name },
                    Environments = new List<string> { environment },
                    BoundsSize = mesh.BoundsSize,
                    BoundsCenter = mesh.BoundsCenter
                });
        }
        else
        {
            var m = list.First(x => x.Hash == mesh.Hash);
            if (!m.Names.Contains(mesh.Name)) m.Names.Add(mesh.Name);
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    public void Sort()
    {
        list = list.OrderBy(x => x.Names.First()).ThenBy(x => x.Hash).ToList();
        for (var index = 0; index < list.Count; index++)
        {
            var m = list[index];
            m.Name = $"{index}: {m.Names.First()}";
        }
    }

    public Mesh GetSafe(string n) => n == "null" ? null : Lookup.GetValueOrDefault(n);
}

[Serializable]
public class MeshInfo
{
    public string Name;
    public string Hash;
    public Mesh Mesh;
    public List<string> Names;
    public List<string> Environments;

    public Vector3 BoundsSize;
    public Vector3 BoundsCenter;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
