using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapLoader : MonoBehaviour
{
    [SerializeField] TracksManager manager;
    [SerializeField] NoteLanesController noteLanesController;
    [Space]
    [SerializeField] Transform containerCollectionsContainer;

    private BeatSaberMap map;
    private int noteLaneSize = 2;
    private int noteLayerSize = 3;

    public void UpdateMapData(BeatSaberMap map)
    {
        BeatSaberMap copy = new BeatSaberMap();
        copy._notes = new List<BeatmapNote>(map._notes);
        copy._obstacles = new List<BeatmapObstacle>(map._obstacles);
        copy._events = new List<MapEvent>(map._events);
        copy._BPMChanges = new List<BeatmapBPMChange>(map._BPMChanges);
        copy._customEvents = new List<BeatmapCustomEvent>(map._customEvents);
        this.map = copy;
    }

    public IEnumerator HardRefresh()
    {
        if (Settings.Instance.Load_Notes) yield return StartCoroutine(LoadObjects(map._notes));
        if (Settings.Instance.Load_Obstacles) yield return StartCoroutine(LoadObjects(map._obstacles));
        if (Settings.Instance.Load_Events) yield return StartCoroutine(LoadObjects(map._events));
        if (Settings.Instance.Load_Others)
        {
            yield return StartCoroutine(LoadObjects(map._BPMChanges));
            yield return StartCoroutine(LoadObjects(map._customEvents));
        }
        PersistentUI.Instance.LevelLoadSliderLabel.text = "Finishing up...";
        manager.RefreshTracks();
        SelectionController.RefreshMap();
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : BeatmapObject
    {
        if (!objects.Any()) yield break;
        BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(objects.First().beatmapType);
        if (collection == null) yield break;
        foreach (BeatmapObject obj in collection.LoadedObjects.ToArray()) collection.DeleteObject(obj);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        collection.LoadedObjects = new SortedSet<BeatmapObject>(objects, new BeatmapObjectComparer());
        collection.UnsortedObjects = collection.LoadedObjects.ToList();
        UpdateSlider<T>();
        collection.RefreshPool();
        if (typeof(T) == typeof(BeatmapNote) || typeof(T) == typeof(BeatmapObstacle))
        {
            for (int i = 0; i < objects.Count(); i++)
            {
                BeatmapObject data = objects.ElementAt(i);
                if (data is BeatmapNote noteData)
                {
                    if (noteData._lineIndex >= 1000 || noteData._lineIndex <= -1000 || noteData._lineLayer >= 1000 || noteData._lineLayer <= -1000) continue;
                    if (2 - noteData._lineIndex > noteLaneSize) noteLaneSize = 2 - noteData._lineIndex;
                    if (noteData._lineIndex - 1 > noteLaneSize) noteLaneSize = noteData._lineIndex - 1;
                    if (noteData._lineLayer + 1 > noteLayerSize) noteLayerSize = noteData._lineLayer + 1;
                }
                else if (data is BeatmapObstacle obstacleData)
                {
                    if (obstacleData._lineIndex >= 1000 || obstacleData._lineIndex <= -1000) continue;
                    if (2 - obstacleData._lineIndex > noteLaneSize) noteLaneSize = 2 - obstacleData._lineIndex;
                    if (obstacleData._lineIndex - 1 > noteLaneSize) noteLaneSize = obstacleData._lineIndex - 1;
                }
            }
            if (Settings.NonPersistentSettings.ContainsKey("NoteLanes"))
            {
                Settings.NonPersistentSettings["NoteLanes"] = (noteLaneSize * 2).ToString();
            }
            else
            {
                Settings.NonPersistentSettings.Add("NoteLanes", (noteLaneSize * 2).ToString());
            }
            noteLanesController.UpdateNoteLanes((noteLaneSize * 2).ToString());
        }
        if (objects.First() is MapEvent)
        {
            manager.RefreshTracks();
            EventsContainer events = collection as EventsContainer;
            events.AllRotationEvents = objects.Cast<MapEvent>().Where(x => x.IsRotationEvent).ToList();
        }
    }

    private void UpdateSlider<T>() where T : BeatmapObject
    {
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        PersistentUI.Instance.LevelLoadSlider.value = 1;
    }
}
