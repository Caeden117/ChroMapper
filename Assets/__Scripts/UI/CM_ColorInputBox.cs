using System;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

public class CM_ColorInputBox : MenuBase
{
    [SerializeField] private ColorPicker ColorInputField;
    [SerializeField] private TextMeshProUGUI UIMessage;
    [SerializeField] private CanvasGroup group;
    private Action<Color?> resultAction;

    private IEnumerable<Type> disabledActionMaps = typeof(CMInput).GetNestedTypes().Where(t => t.IsInterface && t != typeof(CMInput.IUtilsActions) && t != typeof(CMInput.IMenusExtendedActions));

    public bool IsEnabled => group.alpha == 1;

    public void SetParams(string message, Action<Color?> result, Color selectedColor, string defaultText = "")
    {
        if (IsEnabled)
            throw new Exception("Input box is already enabled! Please wait until this Input Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(typeof(CM_ColorInputBox), disabledActionMaps);
        UpdateGroup(true);
        CameraController.ClearCameraMovement();
        ColorInputField.CurrentColor = selectedColor;
        UIMessage.text = message;
        resultAction = result;
    }

    public void EndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendResult(0);
        }
    }

    public void SendResult(int buttonID)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(CM_ColorInputBox), disabledActionMaps);
        UpdateGroup(false);
        Color res = ColorInputField.CurrentColor;
        if (buttonID == 0)
        {
            resultAction?.Invoke(res);
        }
        else
        {
            resultAction?.Invoke(null);
        }
        
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
        EventSystem.current.SetSelectedGameObject(ColorInputField.gameObject, new BaseEventData(EventSystem.current));
    }

    public override void OnTab(InputAction.CallbackContext context)
    {
        if (IsEnabled) base.OnTab(context);
    }

    protected override GameObject GetDefault()
    {
        return ColorInputField.gameObject;
    }

    public override void OnLeaveMenu(InputAction.CallbackContext context)
    {
        SendResult(1);
    }
}
