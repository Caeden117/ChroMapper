using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformToggleDisableableObjects : MonoBehaviour, CMInput.IPlatformDisableableObjectsActions
{
    private PlatformDescriptor descriptor;

    // Start is called before the first frame update
    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

    public void OnTogglePlatformObjects(InputAction.CallbackContext context)
    {
        if (context.performed) UpdateDisableableObjects();
    }

    private void PlatformLoaded(PlatformDescriptor obj) {
        descriptor = obj;
        if (Settings.Instance.HideDisablableObjectsOnLoad) UpdateDisableableObjects();
    }

    public void UpdateDisableableObjects() => descriptor.ToggleDisablableObjects();
}
