using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CM_DialogBox : MonoBehaviour
{
    [FormerlySerializedAs("UIButton")] [SerializeField] private Button uiButton;
    [FormerlySerializedAs("UIMessage")] [SerializeField] private TextMeshProUGUI uiMessage;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_FontAsset defaultFont;


    private readonly IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t =>
        t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IMenusExtendedActions));

    private readonly List<Button> tempButtons = new List<Button>();
    private Action<int> resultAction;

    public bool IsEnabled => group.alpha == 1;

    private void Start() => uiButton.onClick.AddListener(() => SendResult(0));

    public void SetParams(string message, Action<int> result,
        string[] buttonText, TMP_FontAsset[] buttonAsset)
    {
        if (IsEnabled)
            throw new Exception("Dialog box is already enabled! Please wait until this Dialog Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(typeof(CM_DialogBox), disabledActionMaps);
        UpdateGroup(true);
        CameraController.ClearCameraMovement();

        // Ignore yes/no colours for the dark theme and just use the default (no-outline) font

        uiMessage.text = message;
        resultAction = result;
        for (var i = 0; i < buttonText.Length; i++)
        {
            SetupButton(
                i,
                buttonText[i],
                Settings.Instance.DarkTheme || buttonAsset == null ? defaultFont : buttonAsset[i],
                buttonText.Length > 3 ? 80 : 100
            );
        }

        // Make sure any extra buttons are hidden
        for (var i = buttonText.Length; i < tempButtons.Count + 1; i++) SetupButton(i, null, null);
    }

    private void SetupButton(int index, string text, TMP_FontAsset font, int width = 100)
    {
        Button buttonComponent;
        if (index == 0)
        {
            buttonComponent = uiButton;
        }
        else
        {
            if (index > tempButtons.Count)
            {
                var newButton = Instantiate(uiButton.gameObject, uiButton.transform.parent);
                buttonComponent = newButton.GetComponent<Button>();
                buttonComponent.onClick.AddListener(() => SendResult(index));
                tempButtons.Add(buttonComponent);
            }
            else
            {
                buttonComponent = tempButtons[index - 1];
            }
        }

        SetupButton(buttonComponent, text, font, width);
    }

    private void SetupButton(Button button, string text, TMP_FontAsset font, int width)
    {
        button.gameObject.SetActive(text != null);
        button.GetComponent<LayoutElement>().preferredWidth = width;
        button.GetComponentInChildren<TextMeshProUGUI>().text = text ?? "";
        button.GetComponentInChildren<TextMeshProUGUI>().font = font != null ? font : defaultFont;
    }

    public void SendResult(int buttonID)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CM_DialogBox), disabledActionMaps);
        UpdateGroup(false);
        resultAction?.Invoke(buttonID);
    }

    private void UpdateGroup(bool visible)
    {
        group.alpha = visible ? 1 : 0;
        StartCoroutine(WaitAndChangeInteractive(visible));
        group.blocksRaycasts = visible;
    }

    private IEnumerator WaitAndChangeInteractive(bool visible)
    {
        yield return new WaitForSeconds(0.25f);
        group.interactable = visible;
    }
}
