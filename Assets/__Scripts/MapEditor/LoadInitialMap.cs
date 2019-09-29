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

    void Awake()
    {
        SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());
    }

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        yield return new WaitUntil(() => atsc.gridStartPosition != -1); //I need a way to find out when Start has been called.
        song = BeatSaberSongContainer.Instance.song;
        float offset = 0;
        int environmentID = 0;
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
        try {
            map = BeatSaberSongContainer.Instance.map;
            offset = (song.beatsPerMinute / 60) * (BeatSaberSongContainer.Instance.song.songTimeOffset / 1000);
            int noteLaneSize = 2; //Half of it, anyways
            int noteLayerSize = 3;
            if (map != null) {
                foreach (BeatmapNote noteData in map._notes) {
                    BeatmapNoteContainer beatmapNote = notesContainer.SpawnObject(noteData) as BeatmapNoteContainer;
                    if (noteData._lineIndex >= 1000 || noteData._lineIndex <= -1000 || noteData._lineLayer >= 1000 || noteData._lineLayer <= -1000) continue;
                    if (2 - noteData._lineIndex > noteLaneSize) noteLaneSize = 2 - noteData._lineIndex;
                    if (noteData._lineIndex - 1 > noteLaneSize) noteLaneSize = noteData._lineIndex - 1;
                    if (noteData._lineLayer + 1 > noteLayerSize) noteLayerSize = noteData._lineLayer + 1;
                }
                foreach (BeatmapObstacle obstacleData in map._obstacles)
                {
                    BeatmapObstacleContainer beatmapObstacle = obstaclesContainer.SpawnObject(obstacleData) as BeatmapObstacleContainer;
                    if (obstacleData._lineIndex >= 1000 || obstacleData._lineIndex <= -1000) continue;
                    if (2 - obstacleData._lineIndex > noteLaneSize) noteLaneSize = 2 - obstacleData._lineIndex;
                    if (obstacleData._lineIndex - 1 > noteLaneSize) noteLaneSize = obstacleData._lineIndex - 1;
                }
                foreach (MapEvent eventData in map._events) eventsContainer.SpawnObject(eventData);
                foreach (BeatmapBPMChange bpmData in map._BPMChanges) bpmContainer.SpawnObject(bpmData);
                notesContainer.SortObjects();
                obstaclesContainer.SortObjects();
                eventsContainer.SortObjects();
                bpmContainer.SortObjects();
                noteGrid.localScale = new Vector3((float)(noteLaneSize * 2) / 10 + 0.01f, 1, 1);
            }

        } catch (Exception e) {
            Debug.LogWarning("No mapping for you!");
            Debug.LogException(e);
        }
    }
}
