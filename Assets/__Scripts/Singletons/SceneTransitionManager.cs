using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour {

    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance {
        get { return _instance; }
    }

    private static Queue<IEnumerator> externalRoutines = new Queue<IEnumerator>();

    private Coroutine LoadingCoroutine; //For stopping.

    private void Awake() {
        if (_instance != null) {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        _instance = this;
    }

    public void LoadScene(int scene, params IEnumerator[] routines) {
        if (isLoading) return;
        isLoading = true;
        externalRoutines.Clear();
        foreach (IEnumerator routine in routines) externalRoutines.Enqueue(routine);
        LoadingCoroutine = StartCoroutine(SceneTransition(scene));
    }

    public void CancelLoading(string message)
    {
        if (!isLoading || LoadingCoroutine == null) return; //LoadingCoroutine set when LoadScene is called, null when this is called, or SceneTransition finishes.
        StopCoroutine(LoadingCoroutine);
        isLoading = false;
        LoadingCoroutine = null;
        StartCoroutine(CancelLoadingTransitionAndDisplay(message));
    }

    public void AddLoadRoutine(IEnumerator routine) {
        if (isLoading) externalRoutines.Enqueue(routine);
    }

    public void AddAsyncLoadRoutine(IEnumerator routine) {
        if (isLoading) externalRoutines.Enqueue(routine);
    }

    private bool isLoading = false;
    private IEnumerator SceneTransition(int scene) {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return StartCoroutine(RunExternalRoutines());
        //foreach (IEnumerator routine in routines) yield return StartCoroutine(routine);
        yield return SceneManager.LoadSceneAsync(scene);
        //yield return new WaitForSeconds(1f);
        yield return StartCoroutine(RunExternalRoutines()); //We need to do this a second time in case any classes registered a routine to run on scene start.
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
        isLoading = false;
        LoadingCoroutine = null;
    }

    private IEnumerator RunExternalRoutines() {
        //This block runs the routines one by one, which isn't ideal
        while (externalRoutines.Count > 0)
            yield return StartCoroutine(externalRoutines.Dequeue());
    }

    private IEnumerator CancelLoadingTransitionAndDisplay(string message)
    {
        PersistentUI.Instance.DisplayMessage(message, PersistentUI.DisplayMessageType.BOTTOM);
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

}
