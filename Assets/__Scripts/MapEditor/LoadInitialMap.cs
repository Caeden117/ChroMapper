using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LoadInitialMap : MonoBehaviour {

    [SerializeField] Transform noteGrid;
    [SerializeField] AudioTimeSyncController atsc;
    [Space]
    [SerializeField] NotesContainer notesContainer;
    [SerializeField] ObstaclesContainer obstaclesContainer;
    [SerializeField] EventsContainer eventsContainer;
    [SerializeField] BPMChangesContainer bpmContainer;
    [Space]
    [SerializeField] GameObject[] PlatformPrefabs;
    [SerializeField] GameObject[] CustomPlatformPrefabs;

    public static Action<PlatformDescriptor> PlatformLoadedEvent;

    private BeatSaberMap map;
    private BeatSaberSong song;
    private int totalObjectsToLoad = 0;
    private int totalObjectsLoaded = 0;

    void Awake()
    {
        SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());
    }

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        yield return new WaitUntil(() => atsc.gridStartPosition != -1); //I need a way to find out when Start has been called.
        song = BeatSaberSongContainer.Instance.song;
        float offset = 0;
        int environmentID = 0;
        int batchSize = Settings.Instance.InitialLoadBatchSize;
        bool customPlat = false;
        environmentID = SongInfoEditUI.GetEnvironmentIDFromString(song.environmentName);
        if (song.customData != null && song.customData["_customEnvironment"] != null && song.customData["_customEnvironment"].Value != "")
        {
            environmentID = SongInfoEditUI.GetCustomPlatformsIndexFromString(song.customData["_customEnvironment"]);
            customPlat = true;
        }
        GameObject platform = (customPlat ? CustomPlatformPrefabs[environmentID] : PlatformPrefabs[environmentID]) ?? PlatformPrefabs[0];
        GameObject instantiate = Instantiate(platform, new Vector3(0, -0.5f, -1.5f), Quaternion.identity);
        PlatformDescriptor descriptor = instantiate.GetComponent<PlatformDescriptor>();
        BeatmapEventContainer.ModifyTypeMode = descriptor.SortMode;
        PlatformLoadedEvent.Invoke(descriptor);
        descriptor.RedColor = BeatSaberSongContainer.Instance.difficultyData.colorLeft;
        descriptor.BlueColor = BeatSaberSongContainer.Instance.difficultyData.colorRight;
        map = BeatSaberSongContainer.Instance.map;
        offset = (song.beatsPerMinute / 60) * (BeatSaberSongContainer.Instance.song.songTimeOffset / 1000);
        int noteLaneSize = 2; //Half of it, anyways
        int noteLayerSize = 3;
        Queue<BeatmapObject> queuedData = new Queue<BeatmapObject>( //Take all of our object data and combine them for batch loading.
            map._notes.Concat<BeatmapObject>(map._obstacles).Concat(map._events).Concat(map._BPMChanges));
        totalObjectsToLoad = queuedData.Count;
        if (map != null)
        {
            while (queuedData.Count > 0)
            {
                for (int i = 0; i < batchSize; i++)
                {
                    if (queuedData.Count == 0) break;
                    BeatmapObject data = queuedData.Dequeue();
                    if (data is BeatmapNote noteData)
                    {
                        BeatmapNoteContainer beatmapNote = notesContainer.SpawnObject(noteData) as BeatmapNoteContainer;
                        if (noteData._lineIndex >= 1000 || noteData._lineIndex <= -1000 || noteData._lineLayer >= 1000 || noteData._lineLayer <= -1000) continue;
                        if (2 - noteData._lineIndex > noteLaneSize) noteLaneSize = 2 - noteData._lineIndex;
                        if (noteData._lineIndex - 1 > noteLaneSize) noteLaneSize = noteData._lineIndex - 1;
                        if (noteData._lineLayer + 1 > noteLayerSize) noteLayerSize = noteData._lineLayer + 1;
                    }
                    else if (data is BeatmapObstacle obstacleData)
                    {
                        BeatmapObstacleContainer beatmapObstacle = obstaclesContainer.SpawnObject(obstacleData) as BeatmapObstacleContainer;
                        if (obstacleData._lineIndex >= 1000 || obstacleData._lineIndex <= -1000) continue;
                        if (2 - obstacleData._lineIndex > noteLaneSize) noteLaneSize = 2 - obstacleData._lineIndex;
                        if (obstacleData._lineIndex - 1 > noteLaneSize) noteLaneSize = obstacleData._lineIndex - 1;
                    }
                    else if (data is MapEvent eventData) eventsContainer.SpawnObject(eventData);
                    else if (data is BeatmapBPMChange bpmData) bpmContainer.SpawnObject(bpmData);
                }
                UpdateSlider(batchSize);
                yield return new WaitForEndOfFrame();
            }
            notesContainer.SortObjects();
            obstaclesContainer.SortObjects();
            eventsContainer.SortObjects();
            bpmContainer.SortObjects();
            noteGrid.localScale = new Vector3((float)(noteLaneSize * 2) / 10 + 0.01f, 1, 1);
        }
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    private void UpdateSlider(int batchSize)
    {
        totalObjectsLoaded += batchSize;
        if (totalObjectsLoaded > totalObjectsToLoad) totalObjectsLoaded = totalObjectsToLoad;
        PersistentUI.Instance.LevelLoadSlider.value = totalObjectsLoaded / (float)totalObjectsToLoad;
    }
}
