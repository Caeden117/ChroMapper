using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class GeometryGridContainer : BeatmapObjectContainerCollection<BaseEnvironmentEnhancement>
{
    [SerializeField] private GameObject geometryPrefab;
    [SerializeField] private GeometryAppearanceSO geometryAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    public override ObjectType ContainerType => ObjectType.EnvironmentEnhancement;

    public Dictionary<BaseEnvironmentEnhancement, GeometryContainer> LoadedGeometry = new();

    protected override void OnObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var eh = obj as BaseEnvironmentEnhancement;
        if (eh.Geometry is JSONNode)
        {
            var container = GeometryContainer.SpawnGeometry(eh, ref geometryPrefab);
            if (container == null) return;
            container.Setup();
            LoadedContainers.Add(eh, container);
            ObjectsWithContainers.Add(eh);
            geometryAppearanceSo.SetGeometryAppearance(container);
        }
    }

    protected override void OnObjectDelete(BaseObject obj, bool inCollection = false)
    {
        var eh = obj as BaseEnvironmentEnhancement;
        if (LoadedContainers.ContainsKey(eh))
        {
            GameObject.Destroy(LoadedContainers[eh].gameObject);
            LoadedContainers.Remove(eh);
            ObjectsWithContainers.Remove(eh);
        }
    }

    public void LoadAll()
    {
        foreach (var to_spawn in MapObjects)
        {
            OnObjectSpawned(to_spawn);
        }
    }

    public override void RefreshPool(bool force)
    {
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
    }

    internal override void SubscribeToCallbacks()
    {
        LoadInitialMap.LevelLoadedEvent += LoadAll;
    }

    internal override void UnsubscribeToCallbacks()
    {
        LoadInitialMap.LevelLoadedEvent -= LoadAll;
    }

    public override ObjectContainer CreateContainer() =>
        null;
};
