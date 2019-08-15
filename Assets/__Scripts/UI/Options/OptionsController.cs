using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private CanvasGroup[] optionBodyCanvasGroups;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;

    private static int initialGroupLoad = 0;

    public static void ShowOptions(int loadGroup = 0)
    {
        initialGroupLoad = loadGroup;
        if (SceneManager.GetSceneByName("04_Options") != null) return;
        SceneManager.LoadScene(4, LoadSceneMode.Additive);
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(FadeIn(2, optionsCanvasGroup));
    }

    public IEnumerator CloseOptions()
    {
        yield return StartCoroutine(Close(2, optionsCanvasGroup));
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
