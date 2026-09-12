using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// why the hell so many thing depend on this, what's wrong with ya'll
public class LoadInitialMap : MonoBehaviour
{
    public static event Action OnLevelLoaded;

    [SerializeField] private BeatmapRuntimeContext context;

    [Space] [SerializeField] private NoteGridContainer noteGridContainer;
    [SerializeField] private ObstacleGridContainer obstacleGridContainer;
    [SerializeField] private ArcGridContainer arcGridContainer;
    [SerializeField] private ChainGridContainer chainGridContainer;
    [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private MapLoader loader;

    private void Awake()
    {
        // Prevent UI Start callbacks from restoring the previously opened map before this map has loaded.
        EditorStateService.BeginMapLoad();
        SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());
    }

    private void Start() => LoadedDifficultySelectController.OnLoadedDifficultyChanged += UpdatePlatformColors;

    private void OnDestroy() => LoadedDifficultySelectController.OnLoadedDifficultyChanged -= UpdatePlatformColors;

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => context.Atsc.Initialized); // Wait until Start has been called

        //Set up some local variables
        var envName = EnvironmentInfoHelper.GetCurrentEnvironment();
        var customPlat = false;

        //Grab platform by name (Official or Custom)
        // if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
        // {
        //     if (CustomPlatformsLoader
        //             .Instance.GetAllEnvironmentIds()
        //             .IndexOf(info.CustomEnvironmentMetadata.Name)
        //         >= 0)
        //     {
        //         customPlat = true;
        //     }
        // }

        //Instantiate platform, grab descriptor
        var platform = context.EnvironmentList.GetEnvironmentOrDefault(envName);

        // if (customPlat)
        //     platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);

        var sceneLoading = SceneManager.LoadSceneAsync(platform.ID, LoadSceneMode.Additive);
        while (!sceneLoading.isDone) yield return null;

        var descriptor = FindAnyObjectByType<EnvironmentDescriptor>();

        context.SetEnvironment(descriptor);

        PopulateColorsFromMapInfo();
        // Map overrides are applied after the environment publishes its palette, so publish again before UI caches stale buttons.
        context.NotifyColorScheme();
        UpdateObjectContainerColors();

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        // RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot requires cloned managed rings to
        // resize movement snapshots before the saved cursor asks LightshowController to render them.
        loader.HardRefreshBeforeEditorStateRestore(context.Descriptor);
        yield return null;
        // Dispatch owner-specific metadata only after map refresh has finished writing controller defaults.
        EditorStateService.LoadMapData(BeatSaberSongContainer.Instance.Info);
        OnLevelLoaded?.Invoke();
    }

    public void PopulateColorsFromMapInfo()
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        var infoColorScheme =
            0 <= infoDifficulty.ColorSchemeIndex
            && infoDifficulty.ColorSchemeIndex < BeatSaberSongContainer.Instance.Info.ColorSchemes.Count
                ? BeatSaberSongContainer.Instance.Info.ColorSchemes[infoDifficulty.ColorSchemeIndex]
                : null;

        if (infoDifficulty.CustomColorLeft != null)
            context.ColorScheme.LeftNoteColor = infoDifficulty.CustomColorLeft.Value;
        else if (infoColorScheme is { OverrideNotes: true })
            context.ColorScheme.LeftNoteColor = infoColorScheme.SaberAColor;
        if (infoDifficulty.CustomColorRight != null)
            context.ColorScheme.RightNoteColor = infoDifficulty.CustomColorRight.Value;
        else if (infoColorScheme is { OverrideNotes: true })
            context.ColorScheme.RightNoteColor = infoColorScheme.SaberBColor;

        if (infoDifficulty.CustomColorObstacle != null)
            context.ColorScheme.ObstacleColor = infoDifficulty.CustomColorObstacle.Value;
        else if (infoColorScheme is { OverrideNotes: true })
            context.ColorScheme.ObstacleColor = infoColorScheme.ObstaclesColor;

        if (infoDifficulty.CustomEnvColorLeft != null)
            context.ColorScheme.EnvironmentLeftColor = infoDifficulty.CustomEnvColorLeft.Value;
        else if (infoDifficulty.CustomColorLeft != null)
            context.ColorScheme.EnvironmentLeftColor = infoDifficulty.CustomColorLeft.Value;
        else if (infoColorScheme is { OverrideLights: true })
            context.ColorScheme.EnvironmentLeftColor = infoColorScheme.EnvironmentColor0;
        if (infoDifficulty.CustomEnvColorRight != null)
            context.ColorScheme.EnvironmentRightColor = infoDifficulty.CustomEnvColorRight.Value;
        else if (infoDifficulty.CustomColorRight != null)
            context.ColorScheme.EnvironmentRightColor = infoDifficulty.CustomColorRight.Value;
        else if (infoColorScheme is { OverrideLights: true })
            context.ColorScheme.EnvironmentRightColor = infoColorScheme.EnvironmentColor1;

        if (infoDifficulty.CustomEnvColorWhite != null)
        {
            context.ColorScheme.EnvironmentWhiteColor =
                infoDifficulty.CustomEnvColorWhite.Value;
        }
        else if (infoColorScheme is { OverrideLights: true, EnvironmentColorW: not null })
            context.ColorScheme.EnvironmentWhiteColor = infoColorScheme.EnvironmentColorW.Value;

        if (infoDifficulty.CustomEnvColorBoostLeft != null)
            context.ColorScheme.EnvironmentLeftBoostColor = infoDifficulty.CustomEnvColorBoostLeft.Value;
        else if (infoDifficulty.CustomEnvColorLeft != null)
            context.ColorScheme.EnvironmentLeftBoostColor = infoDifficulty.CustomEnvColorLeft.Value;
        else if (infoDifficulty.CustomColorLeft != null)
            context.ColorScheme.EnvironmentLeftBoostColor = infoDifficulty.CustomColorLeft.Value;
        else if (infoColorScheme is { OverrideLights: true })
            context.ColorScheme.EnvironmentLeftBoostColor = infoColorScheme.EnvironmentColor0Boost;
        if (infoDifficulty.CustomEnvColorBoostRight != null)
            context.ColorScheme.EnvironmentRightBoostColor = infoDifficulty.CustomEnvColorBoostRight.Value;
        else if (infoDifficulty.CustomEnvColorRight != null)
            context.ColorScheme.EnvironmentRightBoostColor = infoDifficulty.CustomEnvColorRight.Value;
        else if (infoDifficulty.CustomColorRight != null)
            context.ColorScheme.EnvironmentRightBoostColor = infoDifficulty.CustomColorRight.Value;
        else if (infoColorScheme is { OverrideLights: true })
            context.ColorScheme.EnvironmentRightBoostColor = infoColorScheme.EnvironmentColor1Boost;

        if (infoDifficulty.CustomEnvColorBoostWhite != null)
        {
            context.ColorScheme.EnvironmentWhiteBoostColor =
                infoDifficulty.CustomEnvColorBoostWhite.Value;
        }
        else if (infoColorScheme is { OverrideLights: true, EnvironmentColorWBoost: not null })
            context.ColorScheme.EnvironmentWhiteBoostColor = infoColorScheme.EnvironmentColorWBoost.Value;
    }

    private void UpdateObjectContainerColors()
    {
        var leftNoteColor = context.ColorScheme.LeftNoteColor;
        var rightNoteColor = context.ColorScheme.RightNoteColor;
        noteGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        arcGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        chainGridContainer.UpdateColor(leftNoteColor, rightNoteColor);

        obstacleGridContainer.UpdateColor(context.ColorScheme.ObstacleColor);

        eventGridContainer.UpdateColor(
            context.ColorScheme.EnvironmentLeftColor,
            context.ColorScheme.EnvironmentLeftBoostColor,
            context.ColorScheme.EnvironmentRightColor,
            context.ColorScheme.EnvironmentRightBoostColor,
            context.ColorScheme.EnvironmentWhiteColor,
            context.ColorScheme.EnvironmentWhiteBoostColor
        );
    }

    private void UpdatePlatformColors()
    {
        var previousColors = context.ColorScheme.Clone();

        PopulateColorsFromMapInfo();
        UpdateObjectContainerColors();

        // We only want to refresh pools if the colours have changed as refreshing is pretty expensive
        var currentColors = context.ColorScheme;

        var obstacleColorChanged = previousColors.ObstacleColor != currentColors.ObstacleColor;
        if (obstacleColorChanged) obstacleGridContainer.RefreshPool(true);

        var noteColorChanged = previousColors.RightNoteColor != currentColors.RightNoteColor
            || previousColors.LeftNoteColor != currentColors.LeftNoteColor;
        if (noteColorChanged)
        {
            noteGridContainer.RefreshPool(true);
            arcGridContainer.RefreshPool(true);
            chainGridContainer.RefreshPool(true);
        }

        var lightColorChanged = previousColors.EnvironmentRightColor != currentColors.EnvironmentRightColor
            || previousColors.EnvironmentLeftColor != currentColors.EnvironmentLeftColor
            || previousColors.EnvironmentWhiteColor != currentColors.EnvironmentWhiteColor
            || previousColors.EnvironmentRightBoostColor != currentColors.EnvironmentRightBoostColor
            || previousColors.EnvironmentLeftBoostColor != currentColors.EnvironmentLeftBoostColor
            || previousColors.EnvironmentWhiteBoostColor != currentColors.EnvironmentWhiteBoostColor;
        if (lightColorChanged) eventGridContainer.RefreshPool(true);

        context.NotifyColorScheme();
    }
}
