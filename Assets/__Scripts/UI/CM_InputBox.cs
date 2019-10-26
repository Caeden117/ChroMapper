using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CM_InputBox : MonoBehaviour
{
    [SerializeField] private TMP_InputField InputField;
    [SerializeField] private TextMeshProUGUI UIMessage;
    [SerializeField] private CanvasGroup group;
    private Action<string> resultAction;

    public bool IsEnabled
    {
        get
        {
            return group.alpha == 1;
        }
    }

    public void SetParams(string message, Action<string> result, string defaultText = "")
    {
        if (IsEnabled)
            throw new Exception("Input box is already enabled! Please wait until this Input Box has been disabled.");
        UpdateGroup(true);
        UIMessage.text = message;
        InputField.text = defaultText;
        resultAction = result;
    }

    public void SendResult(int buttonID)
    {
        UpdateGroup(false);
        string res = (string.IsNullOrEmpty(InputField.text) || string.IsNullOrWhiteSpace(InputField.text)) ? "" : InputField.text;
        resultAction?.Invoke(buttonID == 0 ? res : "");
    }

    private void UpdateGroup(bool visible)
    {
        group.alpha = visible ? 1 : 0;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
