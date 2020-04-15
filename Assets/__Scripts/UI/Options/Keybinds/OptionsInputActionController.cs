using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class OptionsInputActionController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keybindName;
    [SerializeField] private TMP_InputField keybindInputField;
    private InputAction action = null;
    private bool isInit = false;

    public void Init(InputAction inputAction)
    {
        if (isInit) return;
        action = inputAction;
        keybindName.text = inputAction.name;
        keybindInputField.text = inputAction.GetBindingDisplayString().Split('|')[0];
        isInit = true;
    }

    public void OnKeybindSelected(string _)
    {
        keybindInputField.text = "";
        if (action != null)
        {
            action.Disable(); //Disable our input action since that is required
            var rebind = action.PerformInteractiveRebinding() //Lets preform a rebind!
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(RefreshText)
                .OnCancel(RefreshText)
                .Start();
        }
    }

    private void RefreshText(InputActionRebindingExtensions.RebindingOperation operation)
    {
        keybindInputField.text = operation.action.GetBindingDisplayString().Split('|')[0];
        keybindInputField.DeactivateInputField(true);
        keybindInputField.OnDeselect(new BaseEventData(EventSystem.current));
        keybindInputField.interactable = false;
        keybindInputField.interactable = true;
        operation.Dispose();
        action.Enable();
    }
}
