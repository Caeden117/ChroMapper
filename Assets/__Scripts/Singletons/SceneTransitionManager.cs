using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneTransitionManager : MonoBehaviour
{
    private static readonly Queue<UniTask> externalRoutines = new Queue<UniTask>();

    [FormerlySerializedAs("darkThemeSO")] [SerializeField] private DarkThemeSO darkThemeSo;

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

    public void LoadScene(string scene, params UniTask[] routines)
    {
        if (IsLoading) return;
        darkThemeSo.DarkThemeifyUI();
        Cursor.lockState = CursorLockMode.None;
        IsLoading = true;
        externalRoutines.Clear();
        foreach (var routine in routines) externalRoutines.Enqueue(routine);
        SceneTransition(scene).Forget();
    }

    public void CancelLoading(string message)
    {
        if (!IsLoading) return; 
        // TODO: Use CancellationToken
        IsLoading = false;
        CancelLoadingTransitionAndDisplay(message).Forget();
    }

    public void AddLoadRoutine(UniTask routine)
    {
        if (IsLoading) externalRoutines.Enqueue(routine);
    }

    public void AddAsyncLoadRoutine(UniTask routine)
    {
        if (IsLoading) externalRoutines.Enqueue(routine);
    }

    private async UniTask SceneTransition(string scene)
    {
        await PersistentUI.Instance.FadeInLoadingScreen();
        await RunExternalRoutines();
        await SceneManager.LoadSceneAsync(scene);
        await RunExternalRoutines();
        
        // holy shit just make the UI dark by default already
        darkThemeSo.DarkThemeifyUI();
        OptionsController.IsActive = false;
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        
        await PersistentUI.Instance.FadeOutLoadingScreen();

        IsLoading = false;
    }

    private async UniTask RunExternalRoutines()
    {
        await UniTask.WhenAll(externalRoutines);
        externalRoutines.Clear();
    }

    private async UniTask CancelLoadingTransitionAndDisplay(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var message = LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", key);
            var notification =
                new PersistentUI.MessageDisplayer.NotificationMessage(message, PersistentUI.DisplayMessageType.Bottom);
            await PersistentUI.Instance.DisplayMessage(notification);
        }

        await PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
