using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem;

public class OptionsController : MenuBase
{
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private Canvas optionsCanvas;
    //[SerializeField] private Button iCareForModders; I CARE TOO!!! But like, this just wont work atm.

    public static Action OptionsLoadedEvent;

    public List<CanvasGroup> OptionBodyCanvasGroups;

    private GameObject postProcessingGO;

    public static bool IsActive { get; internal set; }
    private bool IsClosing = false;

    public static void ShowOptions(int loadGroup = 0)
    {
        if (IsActive) return;
        SceneManager.LoadScene("04_Options", LoadSceneMode.Additive);
        CMInputCallbackInstaller.DisableActionMaps(typeof(OptionsController), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
        OptionsLoadedEvent?.Invoke();
        IsActive = true;
    }

    public void Close()
    {
        if (this != null) StartCoroutine(CloseOptions());
    }

    public void GoToURL(string url)
    {
        Application.OpenURL(url);
    }

    private IEnumerator CloseOptions()
    {
        if (IsClosing) yield break;

        IsClosing = true;
        try
        {
            yield return StartCoroutine(Close(2, optionsCanvasGroup));
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(OptionsController), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
            IsActive = false;
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("04_Options"));
        }
        finally
        {
            IsClosing = false;
        }
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
            yield return new WaitForEndOfFrame();
        }
        group.alpha = 1;
        yield return new WaitForEndOfFrame();
        foreach(CanvasGroup notGroup in OptionBodyCanvasGroups.Where(x => x != group))
        {
            notGroup.blocksRaycasts = false;
            notGroup.interactable = false;
            notGroup.alpha = 0;
        }
    }

    IEnumerator Close(float rate, CanvasGroup group)
    {
        float t = 1;
        while (t > 0)
        {
            group.alpha = fadeOutCurve.Evaluate(t);
            t -= Time.deltaTime * rate;
            yield return new WaitForEndOfFrame();
        }
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void ToggleBongo()
    {
        FindObjectsOfType<BongoCat>().FirstOrDefault()?.ToggleBongo();
    }

    protected override GameObject GetDefault()
    {
        return gameObject;
    }

    public override void OnLeaveMenu(InputAction.CallbackContext context)
    {
        if (context.performed) Close();
    }
}
