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
    private BeatSaberSong.DifficultyBeatmap diff;
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
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.gridStartPosition != -1); //I need a way to find out when Start has been called.

        song = BeatSaberSongContainer.Instance.song; //Grab songe data
        diff = BeatSaberSongContainer.Instance.difficultyData;

        //Set up some local variables
        int environmentID = 0;
        int batchSize = Settings.Instance.InitialLoadBatchSize;
        bool customPlat = false;

        environmentID = SongInfoEditUI.GetEnvironmentIDFromString(song.environmentName); //Grab platform by name (Official or Custom)
        if (song.customData != null && song.customData["_customEnvironment"] != null && song.customData["_customEnvironment"].Value != "")
        {
            environmentID = SongInfoEditUI.GetCustomPlatformsIndexFromString(song.customData["_customEnvironment"]);
            customPlat = true;
        }

        //Instantiate platform, grab descriptor
        GameObject platform = (customPlat ? CustomPlatformPrefabs[environmentID] : PlatformPrefabs[environmentID]) ?? PlatformPrefabs[0];
        GameObject instantiate = Instantiate(platform, new Vector3(0, -0.5f, -1.5f), Quaternion.identity);
        PlatformDescriptor descriptor = instantiate.GetComponent<PlatformDescriptor>();
        BeatmapEventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        //Update Colors
        notesContainer.UpdateColor(diff.colorLeft, diff.colorRight);
        obstaclesContainer.UpdateColor(diff.obstacleColor);
        descriptor.RedColor = diff.envColorLeft;
        descriptor.BlueColor = diff.envColorRight;

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform

        map = BeatSaberSongContainer.Instance.map; //Grab map info, do some stuff
        int noteLaneSize = 2; //Half of it, anyways
        int noteLayerSize = 3;

        Queue<BeatmapObject> queuedData = new Queue<BeatmapObject>( //Take all of our object data and combine them for batch loading.
            map._notes.Concat<BeatmapObject>(map._obstacles).Concat(map._events).Concat(map._BPMChanges));
        totalObjectsToLoad = queuedData.Count;
        if (map != null)
        {
            while (queuedData.Count > 0)
            { //Batch loading is loading a certain amount of objects (Batch Size) every frame, so at least ChroMapper remains active.
                for (int i = 0; i < batchSize; i++)
                {
                    if (queuedData.Count == 0) break;
                    BeatmapObject data = queuedData.Dequeue(); //Dequeue and load them into ChroMapper.
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
            notesContainer.SortObjects(); //Sort these boyes.
            obstaclesContainer.SortObjects();
            eventsContainer.SortObjects();
            bpmContainer.SortObjects();
            noteGrid.localScale = new Vector3((float)(noteLaneSize * 2) / 10 + 0.01f, 1, 1); //Set note lanes appropriately
        }
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false); //Disable progress bar
    }

    private void UpdateSlider(int batchSize) //Batch Loading is also so we can get a neat little progress bar set up.
    {
        totalObjectsLoaded += batchSize;
        if (totalObjectsLoaded > totalObjectsToLoad) totalObjectsLoaded = totalObjectsToLoad;
        PersistentUI.Instance.LevelLoadSliderLabel.text =
            $"Loading Level... ({totalObjectsLoaded} / {totalObjectsToLoad} objects loaded," +
            $" {(totalObjectsLoaded / (float)totalObjectsToLoad * 100).ToString("F2")}% complete.)";
        PersistentUI.Instance.LevelLoadSlider.value = totalObjectsLoaded / (float)totalObjectsToLoad;
    }
}
