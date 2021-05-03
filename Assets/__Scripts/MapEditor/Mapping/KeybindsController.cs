using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This KeybindsController class has been reduced to only modifier keys.
/// </summary>
public class KeybindsController : MonoBehaviour, CMInput.IUtilsActions
{
    /// <summary>
    /// The prefix to an action/action map name to signify that it is internal, and should not be rebindable.
    /// </summary>
    public static char InternalKeybindIdentifier = '+';
    /// <summary>
    /// The prefix to an action/action map name to signify that it should not be blocked by more complex keybinds.
    /// </summary>
    public static char PersistentKeybindIdentifier = '_';

    public static bool IsMouseInWindow { get; private set; } = true;

    private static Vector2 mousePos = Vector2.zero;

    private static bool IsMouseInBounds()
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
    }

    public void OnAltModifier(InputAction.CallbackContext context)
    {
    }

    public void OnShiftModifier(InputAction.CallbackContext context)
    {
    }

    public void OnMouseMovement(InputAction.CallbackContext context)
    {
        mousePos = context.ReadValue<Vector2>();
        IsMouseInWindow = IsMouseInBounds();
    }
}
