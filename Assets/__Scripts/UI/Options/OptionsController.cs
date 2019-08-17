using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private CanvasGroup[] optionBodyCanvasGroups;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private Canvas optionsCanvas;

    private GameObject postProcessingGO;

    private static int initialGroupLoad = 0;

    public static void ShowOptions(int loadGroup = 0)
    {
        if (Find<OptionsController>() != null) return;
        initialGroupLoad = loadGroup;
        SceneManager.LoadScene(4, LoadSceneMode.Additive);
    }

    public void UpdateOptionBody(int groupID = 0)
    {
        for (int i = 0; i < optionBodyCanvasGroups.Length; i++)
            if (optionBodyCanvasGroups[i].alpha == 1 && i != groupID) StartCoroutine(Close(2, optionBodyCanvasGroups[i]));
        StartCoroutine(FadeIn(2, optionBodyCanvasGroups[groupID]));
    }

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name == "03_Mapper")
        {
            optionsCanvas.worldCamera = Camera.main;
            Find<PauseManager>()?.TogglePause();
            postProcessingGO = Find<PostProcessVolume>()?.gameObject ?? null;
            postProcessingGO?.SetActive(false);
        }
        UpdateOptionBody(initialGroupLoad);
        yield return StartCoroutine(FadeIn(2, optionsCanvasGroup));
    }

    public void Close()
    {
        StartCoroutine(CloseOptions());
    }

    private IEnumerator CloseOptions()
    {
        yield return StartCoroutine(Close(2, optionsCanvasGroup));
        postProcessingGO?.SetActive(true);
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("04_Options"));
    }

    public static T Find<T>() where T : MonoBehaviour
    {
        if (SceneManager.GetActiveScene().name != "03_Mapper") return null;
        return FindObjectsOfType<T>().FirstOrDefault();
    }

    IEnumerator FadeIn(float rate, CanvasGroup group)
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

    IEnumerator Close(float rate, CanvasGroup group)
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
}
