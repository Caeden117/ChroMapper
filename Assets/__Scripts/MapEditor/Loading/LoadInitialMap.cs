using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class LoadInitialMap : MonoBehaviour
{
    public static Action<PlatformDescriptor> PlatformLoadedEvent;
    public static Action LevelLoadedEvent;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RotationCallbackController rotationController;

    [Space] [SerializeField] private NotesContainer notesContainer;

    [SerializeField] private ObstaclesContainer obstaclesContainer;
    [SerializeField] private ArcsContainer arcsContainer;
    [SerializeField] private ChainsContainer chainsContainer;
    [SerializeField] private MapLoader loader;

    [FormerlySerializedAs("PlatformPrefabs")] [Space] [SerializeField] private GameObject[] platformPrefabs;

    [FormerlySerializedAs("DirectionalPlatformPrefabs")] [SerializeField] private GameObject[] directionalPlatformPrefabs;

    private BeatSaberSong.DifficultyBeatmap diff;
    private BeatSaberSong song;

    private void Awake() => SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.Initialized); //Wait until Start has been called

        song = BeatSaberSongContainer.Instance.Song; //Grab songe data
        diff = BeatSaberSongContainer.Instance.DifficultyData;

        //Set up some local variables
        var environmentID = 0;
        var customPlat = false;
        var directional = false;

        environmentID =
            SongInfoEditUI.GetEnvironmentIDFromString(song
                .EnvironmentName); //Grab platform by name (Official or Custom)
        if (song.CustomData != null && song.CustomData["_customEnvironment"] != null &&
            song.CustomData["_customEnvironment"].Value != "")
        {
            if (CustomPlatformsLoader.Instance.GetAllEnvironmentIds()
                .IndexOf(song.CustomData["_customEnvironment"] ?? "") >= 0)
            {
                customPlat = true;
            }
        }

        if (rotationController.IsActive && diff.ParentBeatmapSet.BeatmapCharacteristicName != "Lawless")
        {
            environmentID = SongInfoEditUI.GetDirectionalEnvironmentIDFromString(song.AllDirectionsEnvironmentName);
            directional = true;
        }

        //Instantiate platform, grab descriptor
        var platform = platformPrefabs[environmentID] == null
            ? platformPrefabs[0]
            : platformPrefabs[environmentID];

        if (customPlat)
        {
            platform = CustomPlatformsLoader.Instance.LoadPlatform(song.CustomData["_customEnvironment"], platform);
        }

        if (directional && !customPlat) platform = directionalPlatformPrefabs[environmentID];

        var instantiate = customPlat ? platform : Instantiate(platform, PlatformOffset, Quaternion.identity);
        var descriptor = instantiate.GetComponent<PlatformDescriptor>();
        BeatmapEventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        descriptor.Colors = descriptor.DefaultColors.Clone();

        //Update Colors
        var leftNote = BeatSaberSong.DefaultLeftNote; //Have default note as base
        if (descriptor.Colors.RedNoteColor != BeatSaberSong.DefaultLeftColor)
            leftNote = descriptor.Colors.RedNoteColor; //Prioritize platforms
        if (diff.ColorLeft != null) leftNote = diff.ColorLeft ?? leftNote; //Then prioritize custom colors

        var rightNote = BeatSaberSong.DefaultRightNote;
        if (descriptor.Colors.BlueNoteColor != BeatSaberSong.DefaultRightColor)
            rightNote = descriptor.Colors.BlueNoteColor;
        if (diff.ColorRight != null) rightNote = diff.ColorRight ?? rightNote;

        notesContainer.UpdateColor(leftNote, rightNote);
        obstaclesContainer.UpdateColor(diff.ObstacleColor ?? BeatSaberSong.DefaultLeftColor);
        if (Settings.Instance.Load_MapV3)
        {
            arcsContainer.UpdateColor(leftNote, rightNote);
            chainsContainer.UpdateColor(leftNote, rightNote);
        }
        if (diff.ColorLeft != null) descriptor.Colors.RedNoteColor = diff.ColorLeft ?? descriptor.Colors.RedNoteColor;
        if (diff.ColorRight != null)
            descriptor.Colors.BlueNoteColor = diff.ColorRight ?? descriptor.Colors.BlueNoteColor;

        //We set light color to envColorLeft if it exists. If it does not exist, but colorLeft does, we use colorLeft.
        //If neither, we use default platform lights.
        if (diff.EnvColorLeft != null) descriptor.Colors.RedColor = diff.EnvColorLeft ?? descriptor.Colors.RedColor;
        else if (diff.ColorLeft != null) descriptor.Colors.RedColor = diff.ColorLeft ?? descriptor.Colors.RedColor;

        //Same thing for envColorRight
        if (diff.EnvColorRight != null) descriptor.Colors.BlueColor = diff.EnvColorRight ?? descriptor.Colors.BlueColor;
        else if (diff.ColorRight != null) descriptor.Colors.BlueColor = diff.ColorRight ?? descriptor.Colors.BlueColor;

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        yield return StartCoroutine(loader.HardRefresh());
        LevelLoadedEvent?.Invoke();
    }
}
