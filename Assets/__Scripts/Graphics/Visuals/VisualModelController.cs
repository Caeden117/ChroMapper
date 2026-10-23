using System;
using System.Collections.Generic;
using System.Linq;
using CustomNotes;
using UnityEngine;
using ZLinq;

public class VisualModelController : VisualController
{
    private static readonly HashSet<VisualModelController> instances = new();
    // These authored shaders add _Cutout before clipping. Unknown shaders retain the native threshold contract.
    // https://github.com/Swifter1243/you_map/tree/0a902cd1b8f7f7765afc80ea19585787f028138e/you_unity/Assets/Shaders/Note
    private static readonly HashSet<string> visibilityCutoutShaders = new()
    {
        "You/ReflectiveNote", "You/GlassNote", "You/PortalNote", "You/FlickeringArrow"
    };
    public Transform ParentTransform;

    [Header("State")] public List<ModelData> Actives = new();
    public List<Renderer> Renderers = new();
    public int MaxCache = 1;
    private readonly Queue<ModelData> cleanupQueue = new();
    private readonly Dictionary<string, ModelData> nameToInstancedObjects = new();
    private bool hasInstantiated;
    private bool markReplace;

    public void Start()
    {
        foreach (var active in Actives.AsValueEnumerable().Where(active => !cleanupQueue.Contains(active)))
        {
            cleanupQueue.Enqueue(active);
            nameToInstancedObjects[active.Name] = active;
        }

        hasInstantiated = true;
    }

    public void OnEnable() => instances.Add(this);
    public void OnDisable() => instances.Remove(this);

    public void OnValidate()
    {
        if (Application.isPlaying) return;
        ParentTransform = transform;
        for (var index = 0; index < Actives.Count; index++)
        {
            var active = Actives[index];
            Actives[index] = new ModelData(active.Name, active.ColliderMesh, active.GameObject);
        }
    }

    public event Action<Mesh, Transform> OnMeshChanged;
    public event Action<Mesh> OnColliderChanged;

    public static void PurgeCachedModel(string modelName)
    {
        var prefix = modelName + "_";
        foreach (var instance in instances.ToArray()) instance.PurgeCachedModelPrefix(prefix);
    }

    public void Cleanup()
    {
        for (var i = 0; i < cleanupQueue.Count - MaxCache; i++)
        {
            var data = cleanupQueue.Dequeue();
            if (nameToInstancedObjects.TryGetValue(data.Name, out var instance) && !instance.GameObject.activeSelf)
            {
                nameToInstancedObjects.Remove(data.Name);
                Destroy(data.GameObject);
            }
            else if (data.GameObject.activeSelf)
                cleanupQueue.Enqueue(data);
            else
                Destroy(data.GameObject);
        }
    }

    public void Set(VisualModelSO vm)
    {
        if (Actives.Count == 1 && CheckExistingActive(vm.Name)) return;
        HandleReset();
        Add(vm);
    }

    public void Set(PrimitiveType type)
    {
        if (Actives.Count == 1 && CheckExistingActive(type.ToString())) return;
        HandleReset();
        Add(type);
    }

    public void Set(GameObject go, Mesh collMesh, string instanceName)
    {
        if (Actives.Count == 1 && CheckExistingActive(instanceName)) return;
        HandleReset();
        Add(go, collMesh, instanceName);
    }

    private bool CheckExistingActive(string instanceName)
    {
        for (var index = 0; index < Actives.Count; index++)
            if (Actives[index].Name == instanceName)
                return true;

        return false;
    }

    private void HandleReset()
    {
        MpbController.Remove(Renderers);
        for (var index = 0; index < Actives.Count; index++)
        {
            var active = Actives[index];
            if (!hasInstantiated)
            {
                if (!cleanupQueue.Contains(active))
                {
                    cleanupQueue.Enqueue(active);
                    nameToInstancedObjects[active.Name] = active;
                }
            }

            active.GameObject.SetActive(false);
        }

        Cleanup();
        Actives.Clear();
        Renderers.Clear();
        markReplace = true;
    }

    private void PurgeCachedModelPrefix(string prefix)
    {
        foreach (var n in nameToInstancedObjects.Keys.AsValueEnumerable().Where(m => m.StartsWith(prefix)).ToArray())
        {
            var data = nameToInstancedObjects[n];
            MpbController.Remove(data.MpbRenderers);
            Actives.RemoveAll(active => active.Name == n);
            Renderers.RemoveAll(renderer => data.MpbRenderers.Contains(renderer));
            nameToInstancedObjects.Remove(n);
            if (data.GameObject != null) Destroy(data.GameObject);
        }

        var retained = cleanupQueue.AsValueEnumerable().Where(data => !data.Name.StartsWith(prefix)).ToArray();
        cleanupQueue.Clear();
        foreach (var data in retained) cleanupQueue.Enqueue(data);
    }

    public void Add(VisualModelSO vm) => Add(vm.Prefab, vm.Collider, vm.Name);

    public void Add(PrimitiveType type)
    {
        var shapeName = type.ToString();
        ModelData data;
        if (nameToInstancedObjects.TryGetValue(shapeName, out var instance) && !instance.GameObject.activeSelf)
            data = instance;
        else
        {
            data = new ModelData(shapeName, GameObject.CreatePrimitive(type));
            data.GameObject.transform.SetParent(ParentTransform);
            cleanupQueue.Enqueue(data);
            nameToInstancedObjects[shapeName] = data;
        }

        AddInstanced(data);
    }

    public void Add(GameObject go, Mesh collMesh, string instanceName)
    {
        ModelData data;
        if (nameToInstancedObjects.TryGetValue(instanceName, out var instance) && !instance.GameObject.activeSelf)
            data = instance;
        else
        {
            data = new ModelData(instanceName, collMesh, Instantiate(go, ParentTransform));
            cleanupQueue.Enqueue(data);
            nameToInstancedObjects[instanceName] = data;
        }

        AddInstanced(data);
    }

    private void AddInstanced(in ModelData data)
    {
        data.GameObject.SetActive(true);
        Actives.Add(data);

        if (data.Renderers.Length == 0) return;

        if (markReplace)
        {
            if (data.OutlineMesh != null)
                OnMeshChanged?.Invoke(data.OutlineMesh.sharedMesh, data.OutlineMesh.transform);
            OnColliderChanged?.Invoke(data.ColliderMesh);
            markReplace = false;
        }

        Renderers.AddRange(data.MpbRenderers);
        var vivify = data.GameObject.GetComponent<VivifyObject>() != null;
        foreach (var renderer in data.MpbRenderers)
            MpbController.Add(renderer, vivify && UsesVisibilityCutout(renderer));
        MpbController.ApplyChanges();
    }

    private static bool UsesVisibilityCutout(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials.Length == 0) return false;
        foreach (var material in materials)
            if (material == null || material.shader == null || material.IsKeywordEnabled("DEBRIS")
                || !visibilityCutoutShaders.Contains(material.shader.name)) return false;
        return true;
    }
}

[Serializable]
public struct ModelData : IEquatable<ModelData>
{
    public string Name;
    public GameObject GameObject;

    public Mesh ColliderMesh;
    public MeshFilter OutlineMesh;

    public Renderer[] Renderers;
    public Renderer[] MpbRenderers;

    public ModelData(string name, Mesh colliderMesh, GameObject gameObject)
    {
        Name = name;

        GameObject = gameObject;
        GameObject.name = name;

        ColliderMesh = colliderMesh;
        Renderers = gameObject.GetComponentsInChildren<Renderer>();
        OutlineMesh = Renderers.Length > 0
            ? Renderers[0].GetComponentInChildren<MeshFilter>()
            : null;
        MpbRenderers = Renderers
            .AsValueEnumerable()
            .Where(r => r.GetComponent<DisableNoteColorOnGameobject>() == null)
            .ToArray();
    }

    public ModelData(string name, GameObject gameObject)
    {
        Name = name;

        GameObject = gameObject;
        GameObject.name = name;

        ColliderMesh = GameObject.GetComponent<MeshFilter>().sharedMesh;
        Renderers = gameObject.GetComponentsInChildren<Renderer>();
        OutlineMesh = Renderers.Length > 0
            ? Renderers[0].GetComponentInChildren<MeshFilter>()
            : null;
        MpbRenderers = Renderers
            .AsValueEnumerable()
            .Where(r => r.GetComponent<DisableNoteColorOnGameobject>() == null)
            .ToArray();
    }

    public bool Equals(ModelData other)
    {
        return Name == other.Name
            && Equals(GameObject, other.GameObject);
    }

    public override bool Equals(object obj) => obj is ModelData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Name, GameObject);
}
