using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OptionsController : MenuBase
{
    //[SerializeField] private Button iCareForModders; I CARE TOO!!! But like, this just wont work atm.

    public static Action OptionsLoadedEvent;
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private AudioClip bongoCatAudioClip;

    public List<CanvasGroup> OptionBodyCanvasGroups;

    private bool isClosing;

    public static bool IsActive { get; internal set; }

    public static void ShowOptions(int loadGroup = 0)
    {
        if (IsActive) return;
        SceneManager.LoadScene("04_Options", LoadSceneMode.Additive);
        CMInputCallbackInstaller.DisableActionMaps(typeof(OptionsController),
            typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
        OptionsLoadedEvent?.Invoke();
        IsActive = true;
    }

    public void Close()
    {
        if (this != null) StartCoroutine(CloseOptions());
    }

    public void GoToURL(string url) => Application.OpenURL(url);

    private IEnumerator CloseOptions()
    {
        if (isClosing) yield break;

        isClosing = true;
        try
        {
            yield return StartCoroutine(Close(2, optionsCanvasGroup));
            CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(OptionsController),
                typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
            IsActive = false;
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("04_Options"));
        }
        finally
        {
            isClosing = false;
        }
    }

    private IEnumerator Close(float rate, CanvasGroup group)
    {
        float t = 1;
        if (!Settings.Instance.InstantEscapeMenuTransitions)
        {
            while (t > 0)
            {
                group.alpha = fadeOutCurve.Evaluate(t);
                t -= Time.deltaTime * rate;
                yield return new WaitForEndOfFrame();
            }
        }

        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void ToggleBongo(int bongoId)
    {
        Settings.Instance.BongoCat = (bongoId == Settings.Instance.BongoCat) ? -1 : bongoId;

        if (Settings.Instance.BongoCat > -1)
        {
            audioUtil.PlayOneShotSound(bongoCatAudioClip);
            PersistentUI.Instance.DisplayMessage("Bongo cat joins the fight!", PersistentUI.DisplayMessageType.Bottom);
        }
        else
        {
            PersistentUI.Instance.DisplayMessage("Bongo cat disabled :(", PersistentUI.DisplayMessageType.Bottom);
        }

        Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.BongoCat), Settings.Instance.BongoCat);
    }

    protected override GameObject GetDefault() => gameObject;

    public override void OnLeaveMenu(InputAction.CallbackContext context)
    {
        if (context.performed) Close();
    }
}
