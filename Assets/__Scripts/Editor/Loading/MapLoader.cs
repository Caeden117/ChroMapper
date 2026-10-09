using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    [Space] [SerializeField] private Transform containerCollectionsContainer;

    private BaseDifficulty map;

    public void DestroyTrackBoundEnvironmentObjects()
    {
        manager.DestroyTrackBoundEnvironmentObjects();

        var geometry = BeatmapObjectContainerCollection
            .GetCollectionForType<GeometryGridContainer, BaseEnvironmentEnhancement>();
        if (geometry != null)
        {
            geometry.ClearSpawnedGeometry();
        }
    }

    public void UpdateMapData(BaseDifficulty m)
    {
        map = m;
        map.ConvertCustomBpmToOfficial();
        manager.IsV2Map = map.MajorVersion == 2;
    }

    public void HardRefresh()
    {
        manager.ResetAnimationTracks();

        LoadObjects(map.BpmEvents);

        if (Settings.Instance.Load_Others)
        {
            LoadObjects(map.CustomEvents);
            LoadEnvironmentEnhancements(map.EnvironmentEnhancements);
        }

        if (Settings.Instance.Load_Notes)
        {
            LoadObjects(map.Notes);
            LoadObjects(map.Arcs);
            LoadObjects(map.Chains);
        }

        if (Settings.Instance.Load_Obstacles) LoadObjects(map.Obstacles);
        if (Settings.Instance.Load_Events)
        {
            LoadObjects(map.Events);
            LoadObjects(map.LightColorEventBoxGroups);
            LoadObjects(map.LightRotationEventBoxGroups);
            LoadObjects(map.LightTranslationEventBoxGroups);
            LoadObjects(map.VfxEventBoxGroups);
        }

        if (Settings.Instance.Load_Notes || Settings.Instance.Load_Obstacles)
        {
            LoadObjects(map.NJSEvents);
            LoadObjects(map.RotationEvents);
        }

        manager.RefreshTracks();
    }

    // RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot requires movement snapshots to observe
    // every ring appended by environment enhancement spawning before AudioTimeSyncController renders saved map time.
    public void HardRefreshBeforeEditorStateRestore(EnvironmentDescriptor descriptor)
    {
        HardRefresh();

        if (Settings.Instance.Load_Others && map.EnvironmentEnhancements.Count > 0)
        {
            descriptor.Reinitialize();
        }
    }

    public void LoadObjects<T>(List<T> objects) where T : BaseObject
    {
        var collection =
            BeatmapObjectContainerCollection.GetCollectionForType<BeatmapObjectContainerCollection<T>, T>();

        if (collection == null) return;

        // Use a stable sort so newly authored events with equal keys keep their insertion order.
        var sorted = objects.OrderBy(it => it, collection.SortComparer).ToList();
        objects.Clear();
        objects.AddRange(sorted);

        collection.MapObjects = objects;

        if (objects is List<BaseEvent> eventsList)
        {
            var events = collection as EventGridContainer;
            // Build and filter the boost lookup index in one load pass without retaining a linear-scan list.
            events.LoadBoostEvents(eventsList);
            events.AllBpmEvents = eventsList.FindAll(it => it.IsBpmEvent());

            events.LinkAllLightEvents();
            events.LinkRingEvents();
        }

        if (objects is List<BaseCustomEvent> customEventsList)
        {
            var events = collection as CustomEventGridContainer;
            events.LoadAll();
        }

        collection.RefreshPool(true);
    }

    public void LoadEnvironmentEnhancements(List<BaseEnvironmentEnhancement> instructions)
    {
        var collection = BeatmapObjectContainerCollection
            .GetCollectionForType<GeometryGridContainer, BaseEnvironmentEnhancement>();
        if (collection == null) return;

        collection.MapObjects = instructions;
        if (beatmapRuntimeContext.Descriptor != null)
            beatmapRuntimeContext.Descriptor.BloomFogParams.ResetToDefaults();
        beatmapRuntimeContext.NotifyEnvironment();

        collection.RefreshPool(true);
    }
}
