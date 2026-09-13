using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment List", fileName = "EnvironmentListSO")]
public class EnvironmentListSO : ScriptableObject
{
    [SerializeField] public List<EnvironmentListInfo> List = new();

    private readonly Dictionary<string, EnvironmentListInfo> lookupID = new();
    private readonly string defaultEnvironment = "DefaultEnvironment";

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        lookupID.Clear();
        foreach (var entry in List) lookupID[entry.ID] = entry;
    }

    public EnvironmentListInfo GetEnvironmentOrDefault(string environment) =>
        lookupID.TryGetValue(environment, out var env) && !env.Ignore ? env : lookupID[defaultEnvironment];

    public void Sort() => List = List.OrderBy(x => x.ID).ToList();
}

[Serializable]
public class EnvironmentListInfo
{
    public string Name;
    public string ID;
    public TrackDefinitionsSO TrackDefinitions;
    public ColorSchemeSO ColorScheme;
    public bool Ignore;
}
