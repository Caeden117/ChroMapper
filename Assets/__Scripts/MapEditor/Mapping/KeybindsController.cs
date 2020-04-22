using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This KeybindsController class has been reduced to only modifier keys.
/// </summary>
public class KeybindsController : MonoBehaviour, CMInput.IUtilsActions
{
    public static bool ShiftHeld { get; private set; } = false;
    public static bool CtrlHeld { get; private set; } = false;
    public static bool AltHeld { get; private set; } = false;
    public static bool AnyCriticalKeys { get => ShiftHeld || CtrlHeld || AltHeld; }

    public void OnControlModifier(InputAction.CallbackContext context)
    {
        CtrlHeld = context.performed;
    }

    public void OnAltModifier(InputAction.CallbackContext context)
    {
        AltHeld = context.performed;
    }

    public void OnShiftModifier(InputAction.CallbackContext context)
    {
        ShiftHeld = context.performed;
    }
}
