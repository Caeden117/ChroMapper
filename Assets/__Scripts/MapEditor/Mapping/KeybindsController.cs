using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     This KeybindsController class has been reduced to only modifier keys.
/// </summary>
public class KeybindsController : MonoBehaviour, CMInput.IUtilsActions
{
    /// <summary>
    ///     The prefix to an action/action map name to signify that it is internal, and should not be rebindable.
    /// </summary>
    public static char InternalKeybindIdentifier = '+';

    /// <summary>
    ///     The prefix to an action/action map name to signify that it should not be blocked by more complex keybinds.
    /// </summary>
    public static char PersistentKeybindIdentifier = '=';

    public static Vector2 MousePosition = Vector2.zero;

    public static bool IsMouseInWindow { get; private set; } = true;

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
        MousePosition = context.ReadValue<Vector2>();
        IsMouseInWindow = IsMouseInBounds();
    }

    private static bool IsMouseInBounds()
    {
#if UNITY_EDITOR
        var gameSize = Handles.GetMainGameViewSize();
        if (MousePosition.x <= 0 || MousePosition.y <= 0 || MousePosition.x >= gameSize.x - 1 || MousePosition.y >= gameSize.y - 1)
            return false;
#else
        if (MousePosition.x <= 0 || MousePosition.y <= 0 || MousePosition.x >= Screen.width - 1 || MousePosition.y >= Screen.height - 1)
        {
            return false;
        }
#endif
        return true;
    }
}
