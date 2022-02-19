using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        LoadInitialMap.PlatformLoadedEvent += LoadPlatform;
    }

    //Thanks to Guidev on YouTube for the original code for planar reflections, which works just fine with Reflection Probes.
    private void Update()
    {
        if (descriptor is null || !Settings.Instance.Reflections) return;
        var camDirWorld = mainCamera.transform.forward;
        var camUpWorld = mainCamera.transform.up;
        var camPosWorld = mainCamera.transform.position;

        var camDirPlane = descriptor.transform.InverseTransformDirection(camDirWorld);
        var camUpPlane = descriptor.transform.InverseTransformDirection(camUpWorld);
        var camPosPlane = descriptor.transform.InverseTransformPoint(camPosWorld);

        camDirPlane.y *= -1f;
        camUpPlane.y *= -1f;
        camPosPlane.y *= -1f;

        camDirWorld = descriptor.transform.TransformDirection(camDirPlane);
        camUpWorld = descriptor.transform.TransformDirection(camUpWorld);
        camPosWorld = descriptor.transform.TransformPoint(camPosPlane);

        transform.position = camPosWorld;
        transform.LookAt(camPosWorld + camDirWorld, camUpWorld);
    }

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= LoadPlatform;

    private void LoadPlatform(PlatformDescriptor obj) => descriptor = obj;
}
