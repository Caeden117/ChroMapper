using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CM_DialogBox : MonoBehaviour
{
    [SerializeField] private Button[] UIButtons;
    [SerializeField] private TextMeshProUGUI UIMessage;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_FontAsset defaultFont;
    private Action<int> resultAction;

    private Type[] disabledActionMaps = new Type[]
    {
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IPlatformDisableableObjectsActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IModifyingSelectionActions),
        typeof(CMInput.IEventUIActions),
        typeof(CMInput.IBookmarksActions),
        typeof(CMInput.ICameraActions),
    };

    public bool IsEnabled => group.alpha == 1;

    public void SetParams(string message, Action<int> result,
        string button0Text = null, string button1Text = null, string button2Text = null,
        TMP_FontAsset button0Asset = null, TMP_FontAsset button1Asset = null, TMP_FontAsset button2Asset = null)
    {
        if (IsEnabled)
            throw new Exception("Dialog box is already enabled! Please wait until this Dialog Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(disabledActionMaps);
        UpdateGroup(true);
        UIMessage.text = message;
        resultAction = result;
        UIButtons[0].gameObject.SetActive(button0Text != null);
        UIButtons[1].gameObject.SetActive(button1Text != null);
        UIButtons[2].gameObject.SetActive(button2Text != null);
        UIButtons[0].GetComponentInChildren<TextMeshProUGUI>().text = button0Text ?? "";
        UIButtons[1].GetComponentInChildren<TextMeshProUGUI>().text = button1Text ?? "";
        UIButtons[2].GetComponentInChildren<TextMeshProUGUI>().text = button2Text ?? "";
        UIButtons[0].GetComponentInChildren<TextMeshProUGUI>().font = button0Asset ?? defaultFont;
        UIButtons[1].GetComponentInChildren<TextMeshProUGUI>().font = button1Asset ?? defaultFont;
        UIButtons[2].GetComponentInChildren<TextMeshProUGUI>().font = button2Asset ?? defaultFont;
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
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
