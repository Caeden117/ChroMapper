using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;

    [Space] [SerializeField] private Transform containerCollectionsContainer;

    private BaseDifficulty map;

    public void UpdateMapData(BaseDifficulty m)
    {
        map = m;
        map.ConvertCustomBpmToOfficial();
    }

    public void HardRefresh()
    {
        LoadObjects(map.BpmEvents);

        if (Settings.Instance.Load_Others)
        {
            LoadObjects(map.CustomEvents);
            LoadObjects(map.EnvironmentEnhancements);
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

        // We need to force sort our objects when loading externally for Binary Search operations and ordered algorithms to work.
        objects.Sort();

        collection.MapObjects = objects;

        if (objects is List<BaseEvent> eventsList)
        {
            var events = collection as EventGridContainer;
            // Build and filter the boost lookup index in one load pass without retaining a linear-scan list.
            events.LoadBoostEvents(eventsList);
            events.AllBpmEvents = eventsList.FindAll(it => it.IsBpmEvent());

            events.LinkAllLightEvents();
        }

        if (objects is List<BaseCustomEvent> customEventsList)
        {
            var events = collection as CustomEventGridContainer;
            events.LoadAll();
        }

        BeatmapRuntimeContext context = null;
        if (objects is List<BaseEnvironmentEnhancement>)
        {
            context = Resources.FindObjectsOfTypeAll<BeatmapRuntimeContext>().FirstOrDefault();
            if (context != null && context.Descriptor != null)
                context.Descriptor.BloomFogParams.ResetToDefaults();
        }

        collection.RefreshPool(true);

        if (context != null) context.NotifyEnvironment();
    }
}
