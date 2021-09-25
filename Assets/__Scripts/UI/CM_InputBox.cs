using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CM_InputBox : MenuBase
{
    [FormerlySerializedAs("InputField")] [SerializeField] private TMP_InputField inputField;
    [FormerlySerializedAs("UIMessage")] [SerializeField] private TextMeshProUGUI uiMessage;
    [SerializeField] private CanvasGroup group;

    private readonly IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t =>
        t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IMenusExtendedActions));

    private Action<string> resultAction;

    public bool IsEnabled => group.alpha == 1;

    public void SetParams(string message, Action<string> result, string defaultText = "")
    {
        if (IsEnabled)
            throw new Exception("Input box is already enabled! Please wait until this Input Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(typeof(CM_InputBox), disabledActionMaps);
        UpdateGroup(true);
        CameraController.ClearCameraMovement();
        uiMessage.text = message;
        inputField.text = defaultText;
        resultAction = result;
    }

    public void EndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) SendResult(0);
    }

    public void SendResult(int buttonID)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CM_InputBox), disabledActionMaps);
        UpdateGroup(false);
        var res = string.IsNullOrWhiteSpace(inputField.text) ? "" : inputField.text;
        resultAction?.Invoke(buttonID == 0 ? res : null);
        resultAction = null;
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

        // Set focus to input field
        EventSystem.current.SetSelectedGameObject(inputField.gameObject, new BaseEventData(EventSystem.current));
    }

    public override void OnTab(InputAction.CallbackContext context)
    {
        if (IsEnabled) base.OnTab(context);
    }

    protected override GameObject GetDefault() => inputField.gameObject;

    public override void OnLeaveMenu(InputAction.CallbackContext context) => SendResult(1);
}
