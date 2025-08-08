using System;
using System.Collections;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.Serialization;

public class LoadInitialMap : MonoBehaviour
{
    public static Action<PlatformDescriptor> PlatformLoadedEvent;
    public static Action<PlatformColors> PlatformColorsRefreshedEvent;
    public static PlatformDescriptor Platform;
    public static Action LevelLoadedEvent;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RotationCallbackController rotationController;

    [FormerlySerializedAs("notesContainer")] [Space] [SerializeField] private NoteGridContainer noteGridContainer;

    [FormerlySerializedAs("obstaclesContainer")] [SerializeField] private ObstacleGridContainer obstacleGridContainer;
    [FormerlySerializedAs("arcsContainer")] [SerializeField] private ArcGridContainer arcGridContainer;
    [FormerlySerializedAs("chainsContainer")] [SerializeField] private ChainGridContainer chainGridContainer;
    
    [SerializeField] private EventGridContainer eventGridContainer;
    
    [SerializeField] private MapLoader loader;

    [FormerlySerializedAs("PlatformPrefabs")] [Space] [SerializeField] private GameObject[] platformPrefabs;

    private void Awake() => SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());

    private void Start() => LoadedDifficultySelectController.LoadedDifficultyChangedEvent += UpdatePlatformColors;

    private void OnDestroy() => LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= UpdatePlatformColors;

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.Initialized); //Wait until Start has been called

        var info = BeatSaberSongContainer.Instance.Info; //Grab songe data
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

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

        //Instantiate platform, grab descriptor
        var platform = platformPrefabs[environmentID] == null
            ? platformPrefabs[0]
            : platformPrefabs[environmentID];

        if (customPlat)
        {
            platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);
        }

        var instantiate = customPlat ? platform : Instantiate(platform, PlatformOffset, Quaternion.identity);
        var descriptor = instantiate.GetComponent<PlatformDescriptor>();
        EventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        PopulateColorsFromMapInfo(descriptor);
        UpdateObjectContainerColors(descriptor.Colors);

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform
        Platform = descriptor;

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        loader.HardRefresh();
        LevelLoadedEvent?.Invoke();
    }

    private static void PopulateColorsFromMapInfo(PlatformDescriptor platformDescriptor)
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        
        platformDescriptor.Colors = platformDescriptor.DefaultColors.Clone();

        if (infoDifficulty.CustomColorLeft != null) platformDescriptor.Colors.RedNoteColor = infoDifficulty.CustomColorLeft.Value;
        if (infoDifficulty.CustomColorRight != null) platformDescriptor.Colors.BlueNoteColor = infoDifficulty.CustomColorRight.Value;

        if (infoDifficulty.CustomColorObstacle != null) platformDescriptor.Colors.ObstacleColor = infoDifficulty.CustomColorObstacle.Value;
        
        if (infoDifficulty.CustomEnvColorLeft != null) platformDescriptor.Colors.RedColor = infoDifficulty.CustomEnvColorLeft.Value;
        if (infoDifficulty.CustomEnvColorRight != null) platformDescriptor.Colors.BlueColor = infoDifficulty.CustomEnvColorRight.Value;
        if (infoDifficulty.CustomEnvColorWhite != null) platformDescriptor.Colors.WhiteColor = infoDifficulty.CustomEnvColorWhite.Value;

        if (infoDifficulty.CustomEnvColorBoostLeft != null) platformDescriptor.Colors.RedBoostColor = infoDifficulty.CustomEnvColorBoostLeft.Value;
        if (infoDifficulty.CustomEnvColorBoostRight != null) platformDescriptor.Colors.BlueBoostColor = infoDifficulty.CustomEnvColorBoostRight.Value;
        if (infoDifficulty.CustomEnvColorBoostWhite != null) platformDescriptor.Colors.WhiteBoostColor = infoDifficulty.CustomEnvColorBoostWhite.Value;
    }

    private void UpdateObjectContainerColors(PlatformColors platformColors)
    {
        var leftNoteColor = platformColors.RedNoteColor;
        var rightNoteColor = platformColors.BlueNoteColor;
        noteGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        arcGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        chainGridContainer.UpdateColor(leftNoteColor, rightNoteColor);

        obstacleGridContainer.UpdateColor(platformColors.ObstacleColor);

        eventGridContainer.UpdateColor(
            platformColors.RedColor, platformColors.RedBoostColor,
            platformColors.BlueColor, platformColors.BlueBoostColor,
            platformColors.WhiteColor, platformColors.WhiteBoostColor
        );
    }

    private void UpdatePlatformColors()
    {
        var previousColors = Platform.Colors.Clone();
        
        PopulateColorsFromMapInfo(Platform);
        UpdateObjectContainerColors(Platform.Colors);
        
        // We only want to refresh pools if the colours have changed as refreshing is pretty expensive
        var currentColors = Platform.Colors;

        var obstacleColorChanged = previousColors.ObstacleColor != currentColors.ObstacleColor;
        if (obstacleColorChanged)
        {
            obstacleGridContainer.RefreshPool(true);
        }
        
        var noteColorChanged = previousColors.BlueNoteColor != currentColors.BlueNoteColor
            || previousColors.RedNoteColor != currentColors.RedNoteColor;
        if (noteColorChanged)
        {
            noteGridContainer.RefreshPool(true);
            arcGridContainer.RefreshPool(true);
            chainGridContainer.RefreshPool(true);
        }
        
        var lightColorChanged = previousColors.BlueColor != currentColors.BlueColor
            || previousColors.RedColor != currentColors.RedColor
            || previousColors.WhiteColor != currentColors.WhiteColor
            || previousColors.BlueBoostColor != currentColors.BlueBoostColor
            || previousColors.RedBoostColor != currentColors.RedBoostColor
            || previousColors.WhiteBoostColor != currentColors.WhiteBoostColor;
        if (lightColorChanged)
        {
            eventGridContainer.RefreshPool(true);
        }
        
        PlatformColorsRefreshedEvent?.Invoke(Platform.Colors);
    }
}
