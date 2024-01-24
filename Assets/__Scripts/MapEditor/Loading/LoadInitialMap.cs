using System;
using System.Collections;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.Serialization;

public class LoadInitialMap : MonoBehaviour
{
    public static Action<PlatformDescriptor> PlatformLoadedEvent;
    public static PlatformDescriptor Platform;
    public static Action LevelLoadedEvent;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RotationCallbackController rotationController;

    [FormerlySerializedAs("notesContainer")] [Space] [SerializeField] private NoteGridContainer noteGridContainer;

    [FormerlySerializedAs("obstaclesContainer")] [SerializeField] private ObstacleGridContainer obstacleGridContainer;
    [FormerlySerializedAs("arcsContainer")] [SerializeField] private ArcGridContainer arcGridContainer;
    [FormerlySerializedAs("chainsContainer")] [SerializeField] private ChainGridContainer chainGridContainer;
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
        EventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        descriptor.Colors = descriptor.DefaultColors.Clone();

        //Update Colors
        var leftNote = BeatSaberSong.DefaultLeftNote; //Have default note as base
        if (descriptor.Colors.RedNoteColor != BeatSaberSong.DefaultLeftColor)
            leftNote = descriptor.Colors.RedNoteColor; //Prioritize platforms
        if (diff.ColorLeft != null) leftNote = diff.ColorLeft.Value; //Then prioritize custom colors

        var rightNote = BeatSaberSong.DefaultRightNote;
        if (descriptor.Colors.BlueNoteColor != BeatSaberSong.DefaultRightColor)
            rightNote = descriptor.Colors.BlueNoteColor;
        if (diff.ColorRight != null) rightNote = diff.ColorRight.Value;

        noteGridContainer.UpdateColor(leftNote, rightNote);
        obstacleGridContainer.UpdateColor(diff.ObstacleColor ?? BeatSaberSong.DefaultLeftColor);
        arcGridContainer.UpdateColor(leftNote, rightNote);
        chainGridContainer.UpdateColor(leftNote, rightNote);
        if (diff.ColorLeft != null) descriptor.Colors.RedNoteColor = diff.ColorLeft.Value;
        if (diff.ColorRight != null) descriptor.Colors.BlueNoteColor = diff.ColorRight.Value;

        if (diff.EnvColorLeft != null) descriptor.Colors.RedColor = diff.EnvColorLeft.Value;
        if (diff.EnvColorRight != null) descriptor.Colors.BlueColor = diff.EnvColorRight.Value;
        if (diff.EnvColorWhite != null) descriptor.Colors.WhiteColor = diff.EnvColorWhite.Value;

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform
        Platform = descriptor;

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        yield return StartCoroutine(loader.HardRefresh());
        LevelLoadedEvent?.Invoke();
    }
}
