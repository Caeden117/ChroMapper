using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneTransitionManager : MonoBehaviour
{
    private static readonly Queue<IEnumerator> ExternalRoutines = new Queue<IEnumerator>();

    [FormerlySerializedAs("darkThemeSO")] [SerializeField] private DarkThemeSO darkThemeSo;

    private Coroutine loadingCoroutine; //For stopping.

    public static bool IsLoading { get; private set; }

    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void LoadScene(string scene, params IEnumerator[] routines)
    {
        if (IsLoading) return;
        darkThemeSo.DarkThemeifyUI();
        IsLoading = true;
        ExternalRoutines.Clear();
        foreach (var routine in routines) ExternalRoutines.Enqueue(routine);
        loadingCoroutine = StartCoroutine(SceneTransition(scene));
    }

    public void CancelLoading(string message)
    {
        if (!IsLoading || loadingCoroutine == null)
            return; //LoadingCoroutine set when LoadScene is called, null when this is called, or SceneTransition finishes.
        StopCoroutine(loadingCoroutine);
        IsLoading = false;
        loadingCoroutine = null;
        StartCoroutine(CancelLoadingTransitionAndDisplay(message));
    }

    public void AddLoadRoutine(IEnumerator routine)
    {
        if (IsLoading) ExternalRoutines.Enqueue(routine);
    }

    public void AddAsyncLoadRoutine(IEnumerator routine)
    {
        if (IsLoading) ExternalRoutines.Enqueue(routine);
    }

    private IEnumerator CancelSongLoadingRoutine()
    {
        while (IsLoading)
        {
            yield return new WaitForEndOfFrame();
            if (Input.GetKey(KeyCode.Escape) && !PersistentUI.Instance.DialogBoxIsEnabled)
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

    private IEnumerator SceneTransition(string scene)
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return StartCoroutine(RunExternalRoutines());
        //foreach (IEnumerator routine in routines) yield return StartCoroutine(routine);
        yield return SceneManager.LoadSceneAsync(scene);
        if (scene.StartsWith("03")) StartCoroutine(CancelSongLoadingRoutine());
        //yield return new WaitForSeconds(1f);
        yield return
            StartCoroutine(
                RunExternalRoutines()); //We need to do this a second time in case any classes registered a routine to run on scene start.
        darkThemeSo.DarkThemeifyUI();
        OptionsController.IsActive = false;
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
        IsLoading = false;
        loadingCoroutine = null;
    }

    private IEnumerator RunExternalRoutines()
    {
        //This block runs the routines one by one, which isn't ideal
        while (ExternalRoutines.Count > 0)
            yield return StartCoroutine(ExternalRoutines.Dequeue());
    }

    private IEnumerator CancelLoadingTransitionAndDisplay(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var message = LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", key);
            var notification =
                new PersistentUI.MessageDisplayer.NotificationMessage(message, PersistentUI.DisplayMessageType.Bottom);
            yield return PersistentUI.Instance.DisplayMessage(notification);
        }

        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
