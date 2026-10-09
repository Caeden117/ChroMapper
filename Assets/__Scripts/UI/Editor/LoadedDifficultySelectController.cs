using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Info;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadedDifficultySelectController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private EnvironmentListSO environmentList;

    public static event Action OnLoadedDifficultyChanged;

    // Checking for unsaved changes before switching
    [SerializeField] private BeatmapActionContainer beatmapActionContainer;
    [SerializeField] private AutoSaveController autoSaveController;

    private int queuedDropdownValue;
    private int previousDropdownValue;

    private List<InfoDifficulty> setDifficulties;

    private void Start()
    {
        // ToList() to keep the ordering the same in case the ordering changes on save (which it currently does)
        setDifficulties = BeatSaberSongContainer.Instance.MapDifficultyInfo.ParentSet.Difficulties.ToList();

        var options = setDifficulties.Select(x => Settings.Instance.DisplayDiffDetailsInEditor
            ? new TMP_Dropdown.OptionData(!string.IsNullOrWhiteSpace(x.CustomLabel) ? x.CustomLabel : x.Difficulty)
            : new TMP_Dropdown.OptionData(x.Difficulty));

        dropdown.options = new List<TMP_Dropdown.OptionData>(options);
        dropdown.value = setDifficulties.IndexOf(BeatSaberSongContainer.Instance.MapDifficultyInfo);
        previousDropdownValue = dropdown.value;

        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null)
        {
            // Disable in MultiMapper
            gameObject.SetActive(false);
            return;
        }

        if (setDifficulties.Count == 1)
        {
            // No other diffs to switch so disable the dropdown
            dropdown.interactable = false;
        }
        else
            dropdown.onValueChanged.AddListener(OnDropdownChange);
    }

    private void OnDropdownChange(int value)
    {
        queuedDropdownValue = value;

        // => Map is dirty
        if (beatmapActionContainer.ContainsUnsavedActions)
        {
            PersistentUI.Instance.ShowDialogBox(
                "Mapper",
                "save.unsaved.changes.switch",
                UnsavedChangesDialogueResult,
                PersistentUI.DialogBoxPresetType.YesNoCancel);
            return;
        }

        StartCoroutine(SelectDifficulty(queuedDropdownValue));
    }

    private void UnsavedChangesDialogueResult(int result)
    {
        if (result == 0) // 0 - Yes
        {
            autoSaveController.Save();
            StartCoroutine(SelectDifficulty(queuedDropdownValue));
        }
        else if (result == 1) // 1 - No
            StartCoroutine(SelectDifficulty(queuedDropdownValue));
        else // 2 - Cancel
            dropdown.SetValueWithoutNotify(previousDropdownValue);
    }

    private IEnumerator SelectDifficulty(int value)
    {
        // If saving, wait until it's done
        while (autoSaveController.IsSaving) ;

        var currentPlatform = EnvironmentInfoHelper.GetCurrentEnvironment();
        
        var infoDifficulty = setDifficulties[value];
        BeatSaberSongContainer.Instance.MapDifficultyInfo = infoDifficulty;
        var nextPlatform = EnvironmentInfoHelper.GetCurrentEnvironment(infoDifficulty);
        
        var customPlat = false;
        // if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
        // {
        //     if (CustomPlatformsLoader
        //             .Instance.GetAllEnvironmentIds()
        //             .IndexOf(info.CustomEnvironmentMetadata.Name)
        //         >= 0)
        //         customPlat = true;
        // }

        // Enhancements mutate native objects and can reparent them out of their scene. Release those targets
        // before track reset, and reload an enhanced environment even when both difficulties select the same one.
        var reloadEnhancedEnvironment = BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Count > 0;
        mapLoader.DestroyTrackBoundEnvironmentObjects();
        if (currentPlatform != nextPlatform || customPlat || reloadEnhancedEnvironment)
        {
            context.SetEnvironment(null);
            var sceneUnload = SceneManager.UnloadSceneAsync(currentPlatform);
            while (!sceneUnload.isDone) yield return null;

            var platform = context.EnvironmentList.GetEnvironmentOrDefault(nextPlatform);

            // if (customPlat)
            //     platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);

            var sceneLoad = SceneManager.LoadSceneAsync(platform.ID, LoadSceneMode.Additive);
            while (!sceneLoad.isDone) yield return null;

            var descriptor = FindAnyObjectByType<EnvironmentDescriptor>();

            context.SetEnvironment(descriptor);

            // this is already handled from LoadedDifficultyChangedEvent it seems
            // LoadInitialMap.PopulateColorsFromMapInfo(descriptor);
            // LoadInitialMap.UpdateObjectContainerColors(descriptor.ColorScheme);

            // amazing spaghetti code
        }

        var newMap = BeatSaberSongUtils.GetMapFromInfoFiles(
            BeatSaberSongContainer.Instance.Info,
            BeatSaberSongContainer.Instance.MapDifficultyInfo);
        BeatSaberSongContainer.Instance.Map = newMap;
        mapLoader.UpdateMapData(newMap);
        mapLoader.HardRefreshBeforeEditorStateRestore(context.Descriptor);

        previousDropdownValue = value;

        // A great way to get ghost objects without this
        beatmapActionContainer.ClearBeatmapActions();
        SelectionController.DeselectAll();

        OnLoadedDifficultyChanged?.Invoke();
    }

    public void Disable() => gameObject.SetActive(false);
}
