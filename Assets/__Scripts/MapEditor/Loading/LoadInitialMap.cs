using System;
using System.Collections;
using Beatmap.Containers;
using Beatmap.Info;
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

    private InfoDifficulty infoDifficulty;
    private BaseInfo info;

    private void Awake() => SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.Initialized); //Wait until Start has been called

        info = BeatSaberSongContainer.Instance.Info; //Grab songe data
        infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        //Set up some local variables
        var environmentID = 0;
        var customPlat = false;
        var directional = false;

        //Grab platform by name (Official or Custom)
        environmentID =
            SongInfoEditUI.GetEnvironmentIDFromString(info.EnvironmentNames[infoDifficulty.EnvironmentNameIndex]); 
        if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
        {
            if (CustomPlatformsLoader.Instance.GetAllEnvironmentIds()
                .IndexOf(info.CustomEnvironmentMetadata.Name) >= 0)
            {
                customPlat = true;
            }
        }

        if (rotationController.IsActive && infoDifficulty.Characteristic != "Lawless")
        {
            environmentID = SongInfoEditUI.GetDirectionalEnvironmentIDFromString(info.AllDirectionsEnvironmentName);
            directional = true;
        }

        //Instantiate platform, grab descriptor
        var platform = platformPrefabs[environmentID] == null
            ? platformPrefabs[0]
            : platformPrefabs[environmentID];

        if (customPlat)
        {
            platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);
        }

        if (directional && !customPlat) platform = directionalPlatformPrefabs[environmentID];

        var instantiate = customPlat ? platform : Instantiate(platform, PlatformOffset, Quaternion.identity);
        var descriptor = instantiate.GetComponent<PlatformDescriptor>();
        EventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        descriptor.Colors = descriptor.DefaultColors.Clone();

        //Update Colors
        var leftNote = DefaultColors.LeftNote; //Have default note as base
        if (descriptor.Colors.RedNoteColor != DefaultColors.Left)
            leftNote = descriptor.Colors.RedNoteColor; //Prioritize platforms
        if (infoDifficulty.CustomColorLeft != null) leftNote = infoDifficulty.CustomColorLeft.Value; //Then prioritize custom colors

        var rightNote = DefaultColors.RightNote;
        if (descriptor.Colors.BlueNoteColor != DefaultColors.Right)
            rightNote = descriptor.Colors.BlueNoteColor;
        if (infoDifficulty.CustomColorRight != null) rightNote = infoDifficulty.CustomColorRight.Value;

        noteGridContainer.UpdateColor(leftNote, rightNote);
        obstacleGridContainer.UpdateColor(infoDifficulty.CustomColorObstacle ?? DefaultColors.Left);
        arcGridContainer.UpdateColor(leftNote, rightNote);
        chainGridContainer.UpdateColor(leftNote, rightNote);
        if (infoDifficulty.CustomColorLeft != null) descriptor.Colors.RedNoteColor = infoDifficulty.CustomColorLeft.Value;
        if (infoDifficulty.CustomColorRight != null) descriptor.Colors.BlueNoteColor = infoDifficulty.CustomColorRight.Value;

        if (infoDifficulty.CustomEnvColorLeft != null) descriptor.Colors.RedColor = infoDifficulty.CustomEnvColorLeft.Value;
        if (infoDifficulty.CustomEnvColorRight != null) descriptor.Colors.BlueColor = infoDifficulty.CustomEnvColorRight.Value;
        if (infoDifficulty.CustomEnvColorWhite != null) descriptor.Colors.WhiteColor = infoDifficulty.CustomEnvColorWhite.Value;

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform
        Platform = descriptor;

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        loader.HardRefresh();
        LevelLoadedEvent?.Invoke();
    }
}
