using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour {

    public static bool IsLoading { get; private set; }

    public static SceneTransitionManager Instance { get; private set; }

    private static Queue<IEnumerator> externalRoutines = new Queue<IEnumerator>();

    private Coroutine LoadingCoroutine; //For stopping.

    [SerializeField] private DarkThemeSO darkThemeSO;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void LoadScene(string scene, params IEnumerator[] routines) {
        if (IsLoading) return;
        darkThemeSO.DarkThemeifyUI();
        IsLoading = true;
        externalRoutines.Clear();
        foreach (IEnumerator routine in routines) externalRoutines.Enqueue(routine);
        LoadingCoroutine = StartCoroutine(SceneTransition(scene));
    }

    public void CancelLoading(string message)
    {
        if (!IsLoading || LoadingCoroutine == null) return; //LoadingCoroutine set when LoadScene is called, null when this is called, or SceneTransition finishes.
        StopCoroutine(LoadingCoroutine);
        IsLoading = false;
        LoadingCoroutine = null;
        StartCoroutine(CancelLoadingTransitionAndDisplay(message));
    }

    public void AddLoadRoutine(IEnumerator routine) {
        if (IsLoading) externalRoutines.Enqueue(routine);
    }

    public void AddAsyncLoadRoutine(IEnumerator routine) {
        if (IsLoading) externalRoutines.Enqueue(routine);
    }

    private IEnumerator CancelSongLoadingRoutine()
    {
        while (IsLoading)
        {
            yield return new WaitForEndOfFrame();
            if (Input.GetKey(KeyCode.Escape) && !PersistentUI.Instance.DialogBox_IsEnabled)
            {
                PersistentUI.Instance.ShowDialogBox("PersistentUI", "songloading",
                    HandleCancelSongLoading, PersistentUI.DialogBoxPresetType.YesNo);
            }
        }
    }

    private void HandleCancelSongLoading(int res)
    {
        if (res == 0)
        {
            StopAllCoroutines();
            IsLoading = false;
            PersistentUI.Instance.LevelLoadSlider.value = 1;
            PersistentUI.Instance.LevelLoadSliderLabel.text = "Canceling...";
            LoadScene("02_SongEditMenu");
        }
    }

    private IEnumerator SceneTransition(string scene) {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return StartCoroutine(RunExternalRoutines());
        //foreach (IEnumerator routine in routines) yield return StartCoroutine(routine);
        yield return SceneManager.LoadSceneAsync(scene);
        if (scene.StartsWith("03")) StartCoroutine(CancelSongLoadingRoutine());
        //yield return new WaitForSeconds(1f);
        yield return StartCoroutine(RunExternalRoutines()); //We need to do this a second time in case any classes registered a routine to run on scene start.
        darkThemeSO.DarkThemeifyUI();
        OptionsController.IsActive = false;
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
        IsLoading = false;
        LoadingCoroutine = null;
    }

    private IEnumerator RunExternalRoutines() {
        //This block runs the routines one by one, which isn't ideal
        while (externalRoutines.Count > 0)
            yield return StartCoroutine(externalRoutines.Dequeue());
    }

    private IEnumerator CancelLoadingTransitionAndDisplay(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var message = LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", key);
            var notification = new PersistentUI.MessageDisplayer.NotificationMessage(message, PersistentUI.DisplayMessageType.BOTTOM);
            yield return PersistentUI.Instance.DisplayMessage(notification);
        }
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

}
