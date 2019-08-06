using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour {

    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance {
        get { return _instance; }
    }

    private static List<IEnumerator> externalRoutines = new List<IEnumerator>();

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
        foreach (IEnumerator routine in routines) externalRoutines.Add(routine);
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
        if (isLoading) externalRoutines.Add(routine);
    }

    public void AddAsyncLoadRoutine(IEnumerator routine) {
        if (isLoading) externalRoutines.Add(routine);
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
        while (externalRoutines.Count > 0) {
            yield return StartCoroutine(externalRoutines[0]);
            externalRoutines.RemoveAt(0);
        }
    }

    private IEnumerator CancelLoadingTransitionAndDisplay(string message)
    {
        PersistentUI.Instance.DisplayMessage(message, PersistentUI.DisplayMessageType.BOTTOM);
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftControl))
        {   //Swapping these keys over to Ctrl so they dont interfere with actual mapping.
            if (Input.GetKeyDown(KeyCode.Alpha0)) LoadScene(0);
            else if (Input.GetKeyDown(KeyCode.Alpha1)) LoadScene(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) LoadScene(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) LoadScene(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) LoadScene(4);
        }
    }

}
