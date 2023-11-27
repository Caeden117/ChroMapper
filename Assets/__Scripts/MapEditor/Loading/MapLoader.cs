using System.Collections;
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

    public IEnumerator HardRefresh()
    {
        yield return StartCoroutine(LoadObjects(map.BpmEvents));
        if (Settings.Instance.Load_Others)
        {
            yield return StartCoroutine(LoadObjects(map.CustomEvents));
        }
        if (Settings.Instance.Load_Notes) yield return StartCoroutine(LoadObjects(map.Notes));
        if (Settings.Instance.Load_Obstacles) yield return StartCoroutine(LoadObjects(map.Obstacles));
        if (Settings.Instance.Load_Events) yield return StartCoroutine(LoadObjects(map.Events));
        if (Settings.Instance.Load_MapV3)
        {
            yield return StartCoroutine(LoadObjects(map.Arcs));
            yield return StartCoroutine(LoadObjects(map.Chains));
        }

        PersistentUI.Instance.LevelLoadSliderLabel.text = "Finishing up...";
        manager.RefreshTracks();

        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    public IEnumerator LoadObjects<T>(List<T> objects) where T : BaseObject
    {
        if (!objects.Any()) yield break;
        
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<BeatmapObjectContainerCollection<T>>(objects.First().ObjectType);
        if (collection == null) yield break;
        
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);

        collection.MapObjects = objects;
        UpdateSlider<T>();

        foreach (var obj in objects) obj.RecomputeSongBpmTime();

        // removed note lane resizing (relied entirely on Mapping Extensions; everyone should be on Noodle now)

        // TODO(Caeden): Remove all of this bullshit
        if (typeof(T) == typeof(BaseEvent))
        {
            manager.RefreshTracks();

            // TODO(Caeden): ugh.
            var events = collection as EventGridContainer;
            events.AllRotationEvents = objects.Cast<BaseEvent>().Where(x => x.IsLaneRotationEvent()).ToList();
            events.AllBoostEvents = objects.Cast<BaseEvent>().Where(x => x.IsColorBoostEvent())
                .ToList();
            events.AllBpmEvents = objects.Cast<BaseEvent>().Where(x => x.IsBpmEvent())
                .ToList();

            events.LinkAllLightEvents();
        }

        if (typeof(T) == typeof(BaseCustomEvent))
        {
            var events = collection as CustomEventGridContainer;
            events.RefreshEventsByTrack();
            events.LoadAnimationTracks();
        }

        collection.RefreshPool(true);
    }

    private void UpdateSlider<T>() where T : BaseObject
    {
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        PersistentUI.Instance.LevelLoadSlider.value = 1;
    }
}
