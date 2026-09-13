using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CreateUtils
{
    public static IEnumerable<EnvironmentData> GetEnvironmentData()
    {
        return GetEnvironmentDataRaw()
            .Select(JsonToEnvironmentData);
    }

    public static IEnumerable<TextAsset> GetEnvironmentDataRaw()
    {
        var results = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Constants.EnvironmentDataPath) && x.EndsWith(".json"))
            .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
            .Where(x => x != null)
            .ToList();
        if (results.Count == 0)
            Debug.LogError($"[EnvironmentTools] No environment JSON files found at '{Constants.EnvironmentDataPath}'. Extract the environment ZIP there first.");
        else
            Debug.Log($"[EnvironmentTools] Found {results.Count} environment data file(s) at '{Constants.EnvironmentDataPath}'.");
        return results;
    }

    public static EnvironmentData JsonToEnvironmentData(TextAsset textAsset) =>
        JsonConvert.DeserializeObject<EnvironmentData>(
            textAsset.text,
            new Vector2ArrayConverter(),
            new Vector3ArrayConverter(),
            new Vector4ArrayConverter(),
            new ColorArrayConverter());

    public static T CreateOrReplace<T>(T obj, string path) where T : Object
    {
        if (!AssetDatabase.AssetPathExists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(obj, path);
        }
        else
            obj = AssetDatabase.LoadAssetAtPath<T>(path);

        return obj;
    }

    public static GameObject GetGameObjectOrNull(
        Dictionary<string, GameObject> chromaIdObjects,
        string id,
        GameObject go)
    {
        if (id == "self") return go;
        return string.IsNullOrEmpty(id) ? null : chromaIdObjects.GetValueOrDefault(id);
    }

    public static bool TryGetGameObjectOrNull(
        Dictionary<string, GameObject> chromaIdObjects,
        string id,
        GameObject dgo,
        out GameObject go)
    {
        if (id == "self")
        {
            go = dgo;
            return true;
        }

        if (!string.IsNullOrEmpty(id)) return chromaIdObjects.TryGetValue(id, out go);
        go = null;
        return false;
    }
}

public class CreateContainer
{
    public EnvironmentData Data;
    public EnvironmentLibrarySO Library;
    public EnvironmentDescriptor Descriptor;

    public Dictionary<string, GameObject> ChromaIdObjects = new();
    public Dictionary<int, EnvironmentComponentData> ComponentInstances = new();
    public Dictionary<int, MonoBehaviour> LightWithIds = new();

    /// <summary>Gets the ID of the environment currently being created.</summary>
    public string EnvironmentId => Data?.Data?.ID
        ?? throw new InvalidOperationException("The create container has no environment ID.");

    /// <summary>Gets a material by its original JSON hash in the current environment.</summary>
    public Material GetMaterialSafe(string materialHash) => Library.Materials.GetSafe(EnvironmentId, materialHash);

    /// <summary>Tries to get a material by its original JSON hash in the current environment.</summary>
    public bool TryGetMaterial(string materialHash, out Material material) =>
        Library.Materials.TryGetMaterial(EnvironmentId, materialHash, out material);

    public GameObject GetGameObjectOrNull(string n) => CreateUtils.GetGameObjectOrNull(ChromaIdObjects, n, null);

    public GameObject GetGameObjectOrNull(string n, GameObject self) =>
        CreateUtils.GetGameObjectOrNull(ChromaIdObjects, n, self);

    public bool TryGetGameObjectOrNull(string n, GameObject self, out GameObject go) =>
        CreateUtils.TryGetGameObjectOrNull(ChromaIdObjects, n, self, out go);

    public T GetComponentOrNull<T>(int instanceId) where T : Component =>
        ComponentInstances.TryGetValue(instanceId, out var component) ? component.Instance as T : null;

    public static Dictionary<int, EnvironmentComponentData> CollectComponentInstances(EnvironmentData data)
    {
        var compInstances = new Dictionary<int, EnvironmentComponentData>();
        foreach (var obj in data.Objects)
        {
            foreach (var fieldInfo in obj.Components.GetType().GetFields())
            {
                if (!fieldInfo.FieldType.IsArray
                    || !typeof(EnvironmentComponentData).IsAssignableFrom(fieldInfo.FieldType.GetElementType()))
                    continue;
                if (fieldInfo.GetValue(obj.Components) is not EnvironmentComponentData[] d) continue;
                foreach (var a in d) compInstances.Add(a.InstanceId, a);
            }
        }

        return compInstances;
    }
}
