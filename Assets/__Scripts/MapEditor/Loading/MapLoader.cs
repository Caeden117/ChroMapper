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

    private List<BeatmapObjectContainerCollection> containers = new List<BeatmapObjectContainerCollection>();

    private BeatSaberMap map;
    private int totalObjectsToLoad = 0;
    private int totalObjectsLoaded = 0;
    private int batchSize = 0;
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
        if (Settings.Instance.Load_Others) yield return StartCoroutine(LoadObjects(map._customEvents));
        manager.RefreshTracks();
        SelectionController.RefreshMap();
    }

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : BeatmapObject
    {
        if (!objects.Any()) yield break;
        BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType((objects.First() as T).beatmapType);
        if (collection == null) yield break;
        foreach (BeatmapObjectContainer obj in new List<BeatmapObjectContainer>(collection.LoadedContainers)) collection.DeleteObject(obj);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        Queue<T> queuedData = new Queue<T>(objects);
        batchSize = Settings.Instance.InitialLoadBatchSize;
        totalObjectsToLoad = queuedData.Count;
        totalObjectsLoaded = 0;
        while (queuedData.Count > 0)
        { //Batch loading is loading a certain amount of objects (Batch Size) every frame, so at least ChroMapper remains active.
            for (int i = 0; i < batchSize; i++)
            {
                if (queuedData.Count == 0) break;
                BeatmapObject data = queuedData.Dequeue(); //Dequeue and load them into ChroMapper.
                BeatmapObjectContainer obj = collection.SpawnObject(data, false, false);
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
            UpdateSlider<T>(batchSize);
            yield return new WaitForEndOfFrame();
        }
        collection.RemoveConflictingObjects();
        if (objects.First() is BeatmapNote || objects.First() is BeatmapObstacle)
            noteLanesController.UpdateNoteLanes((noteLaneSize * 2).ToString());
        if (objects.First() is MapEvent) manager.RefreshTracks();
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }
    private void UpdateSlider<T>(int batchSize) where T : BeatmapObject //Batch Loading is also so we can get a neat little progress bar set up.
    {
        totalObjectsLoaded += batchSize;
        if (totalObjectsLoaded > totalObjectsToLoad) totalObjectsLoaded = totalObjectsToLoad;
        PersistentUI.Instance.LevelLoadSliderLabel.text =
            $"Loading {typeof(T).Name}s... ({totalObjectsLoaded} / {totalObjectsToLoad} objects loaded," +
            $" {(totalObjectsLoaded / (float)totalObjectsToLoad * 100).ToString("F2")}% complete.)";
        PersistentUI.Instance.LevelLoadSlider.value = totalObjectsLoaded / (float)totalObjectsToLoad;
    }
}
