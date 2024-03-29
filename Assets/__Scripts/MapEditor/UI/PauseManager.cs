using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuestDumper;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour, CMInput.IPauseMenuActions
{
    public static bool IsPaused;
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private UIMode uiMode;
    [SerializeField] private AutoSaveController saveController;
    [SerializeField] private GameObject questSaveButton;

    private readonly IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t =>
        t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IPauseMenuActions));

    private PlatformDescriptor platform;
    private UIModeType previousUIModeType = UIModeType.Normal;

    private void Awake()
    {
        questSaveButton.SetActive(Adb.IsAdbInstalled(out _));
    }

    private void Start()
    {
        OptionsController.OptionsLoadedEvent += OptionsLoaded;
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    private void OnDestroy()
    {
        IsPaused = false;
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        OptionsController.OptionsLoadedEvent -= OptionsLoaded;
    }

    public void OnPauseEditor(InputAction.CallbackContext context)
    {
        if (context.performed) TogglePause();
    }

    private void OptionsLoaded()
    {
        if (IsPaused) TogglePause();
    }

    private void PlatformLoaded(PlatformDescriptor descriptor) => platform = descriptor;

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        if (IsPaused)
        {
            CMInputCallbackInstaller.DisableActionMaps(typeof(PauseManager), disabledActionMaps);
            previousUIModeType = UIMode.SelectedMode;
            uiMode.SetUIMode(UIModeType.Normal, false);
        }
        else
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PauseManager), disabledActionMaps);
            uiMode.SetUIMode(previousUIModeType, false);
        }

        StartCoroutine(TransitionMenu());
    }

    public void Quit(bool save) 
    {
        if (save) // Save and Quit button
        {
            saveController.CheckAndSave(AutoSaveController.SaveType.Menu);
        }
        else // Quit button
        {
            PersistentUI.Instance.ShowDialogBox("Mapper", "save", SaveAndExitResult,
                PersistentUI.DialogBoxPresetType.YesNoCancel);
        }
    }

    public void SaveAndExitToMenu() 
    {
        saveController.Save();
        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null)
        {
            // We want to transition to Song List if we're a multi mapping client;
            //    sending to Song Edit screen would allow people full access to the multi mapped song (not supported)
            SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
        }
        else
        {
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
        }
    }

    public void SaveAndQuitCM()
    {
        saveController.Save();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
    }

    public void ExitToMenu() => PersistentUI.Instance.ShowDialogBox("Mapper", "save", 
            SaveAndExitResult, PersistentUI.DialogBoxPresetType.YesNoCancel);

    public void CloseCM() =>
        PersistentUI.Instance.ShowDialogBox("Mapper", "quit.save",
            SaveAndQuitCmResult, PersistentUI.DialogBoxPresetType.YesNoCancel);

    private void SaveAndExitResult(int result)
    {
        if (result == 0) //Left button (ID 0) clicked; the user wants to Save before exiting.
        {
            saveController.CheckAndSave(AutoSaveController.SaveType.Menu);
        }
        else if (result == 1) //Middle button (ID 1) clicked; the user does not want to save before exiting.
        {
            if (BeatSaberSongContainer.Instance.MultiMapperConnection != null)
            {
                // We want to transition to Song List if we're a multi mapping client;
                //    sending to Song Edit screen would allow people full access to the multi mapped song (not supported)
                SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
            }
            else
            {
                SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            }
        }
        //Right button (ID 2) would be clicked; the user does not want to exit the editor after all, so we aint doing shit.
    }

    private void SaveAndQuitCmResult(int result)
    {
        if (result == 0) //Left button (ID 0) clicked; the user wants to Save before exiting.
        {
            saveController.CheckAndSave(AutoSaveController.SaveType.Quit);
        }
        else if (result == 1) //Middle button (ID 1) clicked; the user does not want to save before exiting.
#if UNITY_EDITOR
        {
            EditorApplication.isPlaying = false;
        }
#else
                Application.Quit();
#endif
        //Right button (ID 2) would be clicked; the user does not want to exit the editor after all, so we aint doing shit.
    }

    #region Transitions (Totally not ripped from PersistentUI)

    private IEnumerator TransitionMenu()
    {
        if (IsPaused) yield return FadeInLoadingScreen(loadingCanvasGroup);
        else yield return FadeOutLoadingScreen(loadingCanvasGroup);
    }

    public Coroutine FadeInLoadingScreen(CanvasGroup group) => StartCoroutine(
        FadeInLoadingScreen(Settings.Instance.InstantEscapeMenuTransitions ? 999f : 2f, loadingCanvasGroup));

    private IEnumerator FadeInLoadingScreen(float rate, CanvasGroup group)
    {
        group.blocksRaycasts = true;
        group.interactable = true;
        float t = 0;
        while (t < 1)
        {
            group.alpha = fadeInCurve.Evaluate(t);
            t += Time.deltaTime * rate;
            yield return null;
        }

        group.alpha = 1;
    }

    public Coroutine FadeOutLoadingScreen(CanvasGroup group) =>
        StartCoroutine(FadeOutLoadingScreen(Settings.Instance.InstantEscapeMenuTransitions ? 999f : 2f, group));

    private IEnumerator FadeOutLoadingScreen(float rate, CanvasGroup group)
    {
        float t = 1;
        while (t > 0)
        {
            group.alpha = fadeOutCurve.Evaluate(t);
            t -= Time.deltaTime * rate;
            yield return null;
        }

        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    #endregion
}
