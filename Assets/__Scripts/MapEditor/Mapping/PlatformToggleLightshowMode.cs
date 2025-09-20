using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformToggleLightshowMode : MonoBehaviour
{
    private PlatformDescriptor descriptor;

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

    private void PlatformLoaded(PlatformDescriptor obj) => descriptor = obj;

    public void SetFullLightshowMode() => descriptor.SetLightshowMode(LightshowMode.Full);
    public void SetStaticLightshowMode() => descriptor.SetLightshowMode(LightshowMode.Static);
    public void SetNoneLightshowMode() => descriptor.SetLightshowMode(LightshowMode.None);
}
