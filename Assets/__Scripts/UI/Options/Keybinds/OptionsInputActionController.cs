using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

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
                .OnMatchWaitForAnother(0.1f) //We can have multiple keybinds perform the same action.
                .WithTimeout(1) //Timeout after one second
                .OnComplete(RefreshText) //Refresh our text and re-activate input field when its complete.
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
        foreach (InputBinding binding in operation.action.bindings)
        {
            Debug.Log(binding.effectivePath);
        }
    }
}
