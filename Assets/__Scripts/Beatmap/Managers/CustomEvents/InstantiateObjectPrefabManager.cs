using System;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateObjectPrefabManager
{
    public VivifyAssetBundleManager VivifyAssetBundleManager;
    public TracksManager TracksManager;
    public Action<UnityEngine.Object> BeforeTargetReleased;

    private readonly Dictionary<string, Stack<VivifyObject>> prefabPool = new();
    private readonly Dictionary<CustomEventStateData, VivifyObject> ownerToObject = new();
    private readonly Dictionary<string, List<CustomEventStateData>> idToOwner = new();
    private readonly Dictionary<CustomEventStateData, VivifyObject> retainedObjects = new();
    private bool replaying;

    /// <summary>Enumerates live instances of an ID, optionally restricted to those spawned before an event.</summary>
    /// <param name="id">Authored prefab ID.</param>
    /// <param name="atEvent">Event whose start beat and sequence bound the instance lifetime; null selects all live instances.</param>
    public IEnumerable<VivifyObject> GetObjectById(string id, CustomEventStateData atEvent = null)
    {
        if (!idToOwner.TryGetValue(id, out var owners)) yield break;
        foreach (var owner in owners)
        {
            if (atEvent != null && (owner.StartTime > atEvent.StartTime
                || (owner.StartTime == atEvent.StartTime && owner.Sequence > atEvent.Sequence))) continue;
            var instance = ResolveObject(owner);
            if (instance != null) yield return instance;
        }
    }

    private VivifyObject GetOrCreateObject(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        if (prefabPool.ContainsKey(prefabName) && prefabPool[prefabName].Count > 0) return prefabPool[prefabName].Pop();
        var prefab = VivifyAssetBundleManager.AssetPathToPrefab.GetValueOrDefault(prefabName);
        return prefab == null ? null : UnityEngine.Object.Instantiate(prefab);
    }

    private void RemoveObject(VivifyObject vivifyObject)
    {
        ReleaseTargets(vivifyObject);
        prefabPool.TryAdd(vivifyObject.AssetPath, new Stack<VivifyObject>());
        vivifyObject.Deactivate();
        prefabPool[vivifyObject.AssetPath].Push(vivifyObject);
    }

    private void ReleaseTargets(VivifyObject vivifyObject)
    {
        foreach (var animator in vivifyObject.Animators)
            BeforeTargetReleased?.Invoke(animator);
    }

    public void Reset()
    {
        var ownedObjects = new HashSet<VivifyObject>();

        foreach (var vivifyObject in ownerToObject.Values)
            DestroyOwnedObject(vivifyObject, ownedObjects);

        foreach (var vivifyObject in retainedObjects.Values)
            DestroyOwnedObject(vivifyObject, ownedObjects);

        foreach (var pool in prefabPool.Values)
        {
            foreach (var vivifyObject in pool)
                DestroyOwnedObject(vivifyObject, ownedObjects);
        }

        ClearState();
    }

    private void DestroyOwnedObject(VivifyObject vivifyObject, HashSet<VivifyObject> ownedObjects)
    {
        if (vivifyObject == null || !ownedObjects.Add(vivifyObject)) return;

        ReleaseTargets(vivifyObject);
        vivifyObject.Deactivate();
        UnityEngine.Object.Destroy(vivifyObject.gameObject);
    }

    private void ClearState()
    {
        ownerToObject.Clear();
        idToOwner.Clear();
        prefabPool.Clear();
        retainedObjects.Clear();
        replaying = false;
    }

    /// <summary>Rebuilds same-map lifetimes without destroying pooled or surviving instances.</summary>
    internal void BeginReplay()
    {
        foreach (var pair in ownerToObject) retainedObjects.Add(pair.Key, pair.Value);
        ownerToObject.Clear();
        idToOwner.Clear();
        replaying = true;
    }

    /// <summary>Releases obsolete instances and materializes the surviving prefab lifetimes.</summary>
    internal void CompleteReplay()
    {
        if (!replaying) return;
        foreach (var pair in retainedObjects)
            if (!ownerToObject.ContainsKey(pair.Key) && pair.Value != null) RemoveObject(pair.Value);
        var owners = new List<CustomEventStateData>(ownerToObject.Keys);
        foreach (var owner in owners) ResolveObject(owner);
        retainedObjects.Clear();
        replaying = false;
    }

    public void InstantiateVivifyObject(CustomEventStateData state)
    {
        // Historical lifetimes need no native object unless another event targets them. Resolving
        // survivors after replay avoids activating objects only to destroy them in this same sample.
        ownerToObject.Add(state, null);
        if (!replaying) ResolveObject(state);

        var data = state.Base.Data;
        if (!data.HasKey("id")) return;
        var id = data["id"];
        idToOwner.TryAdd(id, new());
        idToOwner[id].Add(state);
    }

    private VivifyObject ResolveObject(CustomEventStateData state)
    {
        if (ownerToObject[state] != null) return ownerToObject[state];
        var data = state.Base.Data;
        if (!retainedObjects.Remove(state, out var vivifyObject) || vivifyObject == null)
            vivifyObject = GetOrCreateObject(data["asset"]);
        if (vivifyObject == null) return null;

        vivifyObject.transform.SetParent(null, false);
        vivifyObject.Initialize();
        vivifyObject.Activate();

        if (data["scale"] != null) vivifyObject.transform.localScale = data["scale"].ReadVector3(Vector3.one);

        if (data["localPosition"] != null)
            vivifyObject.transform.localPosition = data["localPosition"].ReadVector3(Vector3.zero);
        else if (data["position"] != null) vivifyObject.transform.position = data["position"].ReadVector3(Vector3.zero);

        if (data["localRotation"] != null)
            vivifyObject.transform.localEulerAngles = data["localRotation"].ReadVector3(Vector3.zero);
        else if (data["rotation"] != null)
            vivifyObject.transform.eulerAngles = data["rotation"].ReadVector3(Vector3.zero);

        vivifyObject.SongSynchronize(state.StartSecondTime);
        
        if (data["track"] != null)
            vivifyObject.Animator.AttachToVivify(vivifyObject, data["track"]);

        ownerToObject[state] = vivifyObject;
        return vivifyObject;
    }

    public void RemoveVivifyObjectById(CustomEventStateData state, Func<string, bool> destroyRenderResource = null)
    {
        if (!state.Base.Data.HasKey("id")) return;

        if (state.Base.Data["id"].IsString && destroyRenderResource?.Invoke(state.Base.Data["id"]) != true
            && idToOwner.TryGetValue(state.Base.Data["id"], out var l1))
        {
            foreach (var s in l1) RemoveVivifyObject(s);
            idToOwner.Remove(state.Base.Data["id"]);
        }
        else if (state.Base.Data["id"].IsArray)
        {
            foreach (var (_, id) in state.Base.Data["id"].AsArray)
            {
                if (!id.IsString || destroyRenderResource?.Invoke(id) == true || !idToOwner.TryGetValue(id, out var l2)) continue;
                foreach (var s in l2) RemoveVivifyObject(s);
                idToOwner.Remove(id);
            }
        }
    }

    private void RemoveVivifyObject(CustomEventStateData state)
    {
        if (!ownerToObject.Remove(state, out var instance)) return;
        if (instance == null) retainedObjects.Remove(state, out instance);
        if (instance != null) RemoveObject(instance);
    }
}
