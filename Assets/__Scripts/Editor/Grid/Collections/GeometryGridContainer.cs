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
        if (LoadedContainers.ContainsKey(eh))
        {
            // Must be immediate to prevent light id conflicts
            GameObject.DestroyImmediate(LoadedContainers[eh].gameObject);
            LoadedContainers.Remove(eh);
            ObjectsWithContainers.Remove(eh);
        }
    }

    public override void RefreshPool(bool force)
    {
        if (force)
        {
            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions exposed the forced geometry reset's
            // repeated List.Remove scan; destroy each visual once, then clear both ownership indexes in bulk.
            foreach (var container in LoadedContainers.Values)
            {
                GameObject.DestroyImmediate(container.gameObject);
            }

            LoadedContainers.Clear();
            ObjectsWithContainers.Clear();

            foreach (var to_spawn in MapObjects)
            {
                if (to_spawn.HasMatchingTrack(TrackFilterID))
                {
                    HandleObjectSpawned(to_spawn);
                }
            }
        }
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
