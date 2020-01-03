using UnityEngine;

public class PlatformToggleDisableableObjects : MonoBehaviour
{
    private PlatformDescriptor descriptor;

    // Start is called before the first frame update
    void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    private void PlatformLoaded(PlatformDescriptor obj)
    {
        descriptor = obj;
    }

    public void UpdateDisableableObjects()
    {
        descriptor?.ToggleDisablableObjects();
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    }
}
