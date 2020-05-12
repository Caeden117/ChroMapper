using System;
using UnityEngine;
using TMPro;
using System.Collections;

public class CM_InputBox : MonoBehaviour
{
    [SerializeField] private TMP_InputField InputField;
    [SerializeField] private TextMeshProUGUI UIMessage;
    [SerializeField] private CanvasGroup group;
    private Action<string> resultAction;

    private Type[] disabledActionMaps = new Type[]
    {
        typeof(CMInput.ICameraActions),
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IPlatformDisableableObjectsActions),
        typeof(CMInput.IBookmarksActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IModifyingSelectionActions),
        typeof(CMInput.IEventUIActions),
    };

    public bool IsEnabled => group.alpha == 1;

    public void SetParams(string message, Action<string> result, string defaultText = "")
    {
        if (IsEnabled)
            throw new Exception("Input box is already enabled! Please wait until this Input Box has been disabled.");
        CMInputCallbackInstaller.DisableActionMaps(disabledActionMaps);
        UpdateGroup(true);
        UIMessage.text = message;
        InputField.text = defaultText;
        resultAction = result;
    }

    public void SendResult(int buttonID)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(disabledActionMaps);
        UpdateGroup(false);
        string res = (string.IsNullOrEmpty(InputField.text) || string.IsNullOrWhiteSpace(InputField.text)) ? "" : InputField.text;
        resultAction?.Invoke(buttonID == 0 ? res : null);
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
