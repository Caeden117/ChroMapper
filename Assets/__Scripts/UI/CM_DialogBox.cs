using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CM_DialogBox : MonoBehaviour
{
    [SerializeField] private Button UIButton;
    private List<Button> TempButtons = new List<Button>();
    [SerializeField] private TextMeshProUGUI UIMessage;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_FontAsset defaultFont;
    private Action<int> resultAction;


    private IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t => t.IsInterface && t != typeof(CMInput.IUtilsActions));

    private void Start()
    {
        UIButton.onClick.AddListener(() => SendResult(0));
    }

    public bool IsEnabled => group.alpha == 1;

    public void SetParams(string message, Action<int> result,
        string[] buttonText, TMP_FontAsset[] buttonAsset)
    {
        if (IsEnabled)
            throw new Exception("Dialog box is already enabled! Please wait until this Dialog Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(disabledActionMaps);
        UpdateGroup(true);
        CameraController.ClearCameraMovement();

        // Ignore yes/no colours for the dark theme and just use the default (no-outline) font

        UIMessage.text = message;
        resultAction = result;
        for (int i = 0; i < buttonText.Length; i++)
        {
            SetupButton(
                i,
                buttonText[i],
                Settings.Instance.DarkTheme || buttonAsset == null ? defaultFont : buttonAsset[i],
                buttonText.Length > 3 ? 80 : 100
            );
        }
        
        // Make sure any extra buttons are hidden
        for (int i = buttonText.Length; i < TempButtons.Count + 1; i++)
        {
            SetupButton(i, null, null);
        }
    }

    private void SetupButton(int index, string text, TMP_FontAsset font, int width = 100)
    {
        Button buttonComponent;
        if (index == 0)
        {
            buttonComponent = UIButton;
        }
        else
        {
            if (index > TempButtons.Count)
            {
                var newButton = Instantiate(UIButton.gameObject, UIButton.transform.parent);
                buttonComponent = newButton.GetComponent<Button>();
                buttonComponent.onClick.AddListener(() => SendResult(index));
                TempButtons.Add(buttonComponent);
            }
            else
            {
                buttonComponent = TempButtons[index - 1];
            }
        }

        SetupButton(buttonComponent, text, font, width);
    }

    private void SetupButton(Button button, string text, TMP_FontAsset font, int width)
    {
        button.gameObject.SetActive(text != null);
        button.GetComponent<LayoutElement>().preferredWidth = width;
        button.GetComponentInChildren<TextMeshProUGUI>().text = text ?? "";
        button.GetComponentInChildren<TextMeshProUGUI>().font = font ?? defaultFont;
    }

    public void SendResult(int buttonID)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(disabledActionMaps);
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
