using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;
    [SerializeField] private NoteLanesController noteLanesController;

    [Space][SerializeField] private Transform containerCollectionsContainer;

    private BaseDifficulty map;

    public void UpdateMapData(BaseDifficulty m)
    {
        map = m;
        map.ConvertCustomBpmToOfficial();
    }

    public void HardRefresh()
    {
        LoadObjects(map.BpmEvents);
        
        if (Settings.Instance.Load_Others) LoadObjects(map.CustomEvents);

        if (Settings.Instance.Load_Notes) LoadObjects(map.Notes);
        
        if (Settings.Instance.Load_Obstacles) LoadObjects(map.Obstacles);
        
        if (Settings.Instance.Load_Events) LoadObjects(map.Events);
        
        if (Settings.Instance.Load_MapV3)
        {
            LoadObjects(map.Arcs);
            LoadObjects(map.Chains);
        }

        manager.RefreshTracks();
    }

    public void LoadObjects<T>(List<T> objects) where T : BaseObject
    {
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<BeatmapObjectContainerCollection<T>, T>();

        if (collection == null) return;
        
        // We need to force sort our objects when loading externally for Binary Search operations and ordered algorithms to work.
        objects.Sort();
        
        collection.MapObjects = objects;

        // TODO: speed up with Span<> iteration
        objects.ForEach(obj => obj.RecomputeSongBpmTime());
        
        if (objects is List<BaseEvent> eventsList)
        {
            manager.RefreshTracks();

            var events = collection as EventGridContainer;
            events.AllRotationEvents = eventsList.FindAll(it => it.IsLaneRotationEvent());
            events.AllBoostEvents = eventsList.FindAll(it => it.IsColorBoostEvent());
            events.AllBpmEvents = eventsList.FindAll(it => it.IsBpmEvent());

            events.LinkAllLightEvents();
        }

        if (objects is List<BaseCustomEvent> customEventsList)
        {
            var events = collection as CustomEventGridContainer;
            events.RefreshEventsByTrack();
            events.LoadAnimationTracks();
        }

        collection.RefreshPool(true);
    }
}
