using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GeometryGridContainer : BeatmapObjectContainerCollection<BaseEnvironmentEnhancement>
{
    [SerializeField] private GameObject geometryPrefab;
    [SerializeField] private GeometryAppearanceSO geometryAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    public override ObjectType ContainerType => ObjectType.EnvironmentEnhancement;

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var eh = obj as BaseEnvironmentEnhancement;

        var container = GeometryContainer.SpawnGeometry(
            eh,
            ref geometryPrefab,
            BeatmapContext,
            tracksManager);
        if (container == null) return;
        container.Setup();
        if (LoadedContainers.TryAdd(eh, container)) ObjectsWithContainers.Add(eh);
        geometryAppearanceSo.SetGeometryAppearance(container);
        container.Selected = SelectionController.IsObjectSelected(obj);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        var eh = obj as BaseEnvironmentEnhancement;
        if (LoadedContainers.TryGetValue(eh, out var container))
        {
            GameObject.DestroyImmediate(container.gameObject);
            LoadedContainers.Remove(eh);
            ObjectsWithContainers.Remove(eh);
        }
    }

    public override void RefreshPool(bool force)
    {
        if (force)
        {
            ClearSpawnedGeometry();

            foreach (var to_spawn in MapObjects)
            {
                if (to_spawn.HasMatchingTrack(TrackFilterID))
                {
                    HandleObjectSpawned(to_spawn);
                }
            }
        }
    }

    // Release these registrations before unloading their environment scene, which also destroys untracked geometry.
    public void ClearSpawnedGeometry()
    {
        foreach (var container in LoadedContainers.Values)
        {
            if (container != null)
            {
                GameObject.DestroyImmediate(container.gameObject);
            }
        }

        LoadedContainers.Clear();
        ObjectsWithContainers.Clear();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
    }

    internal override void SubscribeToCallbacks()
    {
    }

    internal override void UnsubscribeToCallbacks()
    {
    }

    public override ObjectContainer CreateContainer() => null;
};
