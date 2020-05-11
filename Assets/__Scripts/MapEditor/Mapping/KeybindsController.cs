using UnityEditor;
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
    public static bool IsMouseInWindow => IsMouseInBounds();

    private static Vector2 mousePos = Vector2.zero;

    public static bool IsMouseInBounds()
    {
#if UNITY_EDITOR
        Vector2 gameSize = Handles.GetMainGameViewSize();
        if (mousePos.x <= 0 || mousePos.y <= 0 || mousePos.x >= gameSize.x - 1 || mousePos.y >= gameSize.y - 1)
        {
            return false;
        }
#else
        if (mousePos.x <= 0 || mousePos.y <= 0 || mousePos.x >= Screen.width - 1 || mousePos.y >= Screen.height - 1)
        {
            return false;
        }
#endif
        else return true;
    }

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

    public void OnMouseMovement(InputAction.CallbackContext context)
    {
        mousePos = context.ReadValue<Vector2>();
    }
}
