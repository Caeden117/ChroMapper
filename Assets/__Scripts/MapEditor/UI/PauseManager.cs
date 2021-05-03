using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour, CMInput.IPauseMenuActions
{
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private UIMode uiMode;
    private UIModeType previousUIModeType = UIModeType.NORMAL;
    private PlatformDescriptor platform;
    [SerializeField] private AutoSaveController saveController;

    private IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t => t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IPauseMenuActions));

    public static bool IsPaused;

    void Start()
    {
        OptionsController.OptionsLoadedEvent += OptionsLoaded;
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    private void OptionsLoaded()
    {
        if (IsPaused) TogglePause();
    }

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platform = descriptor;
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        if (IsPaused)
        {
            CMInputCallbackInstaller.DisableActionMaps(typeof(PauseManager), disabledActionMaps);
            previousUIModeType = uiMode.selectedMode;
            uiMode.SetUIMode(UIModeType.NORMAL, false);
            foreach (LightsManager e in platform.gameObject.GetComponentsInChildren<LightsManager>())
                e.ChangeAlpha(0, 1, e.ControllingLights);
        }
        else
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(PauseManager), disabledActionMaps);
            uiMode.SetUIMode(previousUIModeType, false);
        }
        StartCoroutine(TransitionMenu());
    }

    void OnDestroy()
    {
        IsPaused = false;
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        OptionsController.OptionsLoadedEvent -= OptionsLoaded;
    }

    public void Quit(bool save)
    {
        if (save)
        {
            saveController.Save();
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
        }
        else
            PersistentUI.Instance.ShowDialogBox("Mapper", "save", SaveAndExitResult, PersistentUI.DialogBoxPresetType.YesNoCancel);
    }

    public void CloseCM()
    {
        PersistentUI.Instance.ShowDialogBox("Mapper", "quit.save",
            SaveAndQuitCMResult, PersistentUI.DialogBoxPresetType.YesNoCancel);
    }

    private void SaveAndExitResult(int result)
    {
        if (result == 0) //Left button (ID 0) clicked; the user wants to Save before exiting.
        {
            saveController.Save();
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
        }
        else if (result == 1) //Middle button (ID 1) clicked; the user does not want to save before exiting.
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
        //Right button (ID 2) would be clicked; the user does not want to exit the editor after all, so we aint doing shit.
    }

    private void SaveAndQuitCMResult(int result)
    {
        if (result == 0) //Left button (ID 0) clicked; the user wants to Save before exiting.
        {
            saveController.Save();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        else if (result == 1) //Middle button (ID 1) clicked; the user does not want to save before exiting.
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        //Right button (ID 2) would be clicked; the user does not want to exit the editor after all, so we aint doing shit.
    }

    #region Transitions (Totally not ripped from PersistentUI)
    IEnumerator TransitionMenu()
    {
        if (IsPaused) yield return FadeInLoadingScreen(loadingCanvasGroup);
        else yield return FadeOutLoadingScreen(loadingCanvasGroup);
    }

    public Coroutine FadeInLoadingScreen(CanvasGroup group)
    {
        return StartCoroutine(FadeInLoadingScreen(Settings.Instance.InstantEscapeMenuTransitions ? 999f : 2f, loadingCanvasGroup));
    }

    IEnumerator FadeInLoadingScreen(float rate, CanvasGroup group)
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

    public Coroutine FadeOutLoadingScreen(CanvasGroup group)
    {
        return StartCoroutine(FadeOutLoadingScreen(Settings.Instance.InstantEscapeMenuTransitions ? 999f : 2f, group));
    }

    IEnumerator FadeOutLoadingScreen(float rate, CanvasGroup group)
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

    public void OnPauseEditor(InputAction.CallbackContext context)
    {
        if (context.performed) TogglePause();
    }
}
