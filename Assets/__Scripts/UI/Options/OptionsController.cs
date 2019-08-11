using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;

    public static void ShowOptions()
    {
        if (SceneManager.GetSceneByName("04_Options") != null) return;
        SceneManager.LoadScene(4, LoadSceneMode.Additive);
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(FadeIn(2, optionsCanvasGroup));
    }

    public void CloseOptions()
    {
        StartCoroutine(Close(2, optionsCanvasGroup));
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
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("04_Options"));
    }
}
