using UnityEngine;
using UnityEngine.InputSystem;

public class RemotePlayerInputController : MonoBehaviour, CMInput.IUnitedMappingActions
{
    public void OnKickPlayer(InputAction.CallbackContext context)
    {
        var ray = Camera.main.ScreenPointToRay(KeybindsController.MousePosition);
        if (context.performed && Intersections.Raycast(ray, 13, out var hit))
        {
            var container = hit.GameObject.GetComponentInParent<RemotePlayerContainer>();

            if (container != null)
            {
                container.Kick();
            }
        }
    }

    public void OnBanPlayer(InputAction.CallbackContext context)
    {
        var ray = Camera.main.ScreenPointToRay(KeybindsController.MousePosition);
        if (context.performed && Intersections.Raycast(ray, 13, out var hit))
        {
            var container = hit.GameObject.GetComponentInParent<RemotePlayerContainer>();

            if (container != null)
            {
                container.Ban();
            }
        }
    }
}
