using System;
using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    private Camera mainCamera;
    private PlatformDescriptor descriptor;

    private void Start()
    {
        mainCamera = Camera.main;
        LoadInitialMap.PlatformLoadedEvent += LoadPlatform;
    }

    private void LoadPlatform(PlatformDescriptor obj)
    {
        descriptor = obj;
    }

    //Thanks to Guidev on YouTube for the original code for planar reflections, which works just fine with Reflection Probes.
    void Update()
    {
        if (descriptor is null || !Settings.Instance.Reflections) return;
        Vector3 camDirWorld = mainCamera.transform.forward;
        Vector3 camUpWorld = mainCamera.transform.up;
        Vector3 camPosWorld = mainCamera.transform.position;

        Vector3 camDirPlane = descriptor.transform.InverseTransformDirection(camDirWorld);
        Vector3 camUpPlane = descriptor.transform.InverseTransformDirection(camUpWorld);
        Vector3 camPosPlane = descriptor.transform.InverseTransformPoint(camPosWorld);

        camDirPlane.y *= -1f;
        camUpPlane.y *= -1f;
        camPosPlane.y *= -1f;

        camDirWorld = descriptor.transform.TransformDirection(camDirPlane);
        camUpWorld = descriptor.transform.TransformDirection(camUpWorld);
        camPosWorld = descriptor.transform.TransformPoint(camPosPlane);

        transform.position = camPosWorld;
        transform.LookAt(camPosWorld + camDirWorld, camUpWorld);
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadPlatform;
    }
}
