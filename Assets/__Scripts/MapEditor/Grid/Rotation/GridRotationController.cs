using System;
using System.Collections;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    private static readonly int rotation = Shader.PropertyToID("_Rotation");

    public Action ObjectRotationChangedEvent;
    public RotationCallbackController RotationCallback;

    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    [SerializeField] private bool rotateTransform = true;

    private float targetRotation;
    private float currentRotation;

    private void Start()
    {
        Shader.SetGlobalFloat(rotation, 0);
        if (RotationCallback != null) Init();
    }

    private void LateUpdate()
    {
        if (!Settings.Instance.RotateTrack) return;

        // Changing rotation time to a constant
        ChangeRotation(Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime / 0.15f));
    }

    private void OnDestroy()
    {
        RotationCallback.RotationChangedEvent -= RotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    public void Init()
    {
        enabled = false;

        if (!RotationCallback.IsActive) return;

        enabled = true;
        RotationCallback.RotationChangedEvent += RotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void UpdateRotateTrack(object obj)
    {
        var rotating = (bool)obj;
        if (rotating)
        {
            ChangeRotation(RotationCallback.Rotation);
        }
        else
        {
            ChangeRotation(0);
        }
    }

    private void RotationChanged(bool natural, float rotation)
    {
        if (!RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        targetRotation = rotation;
        if (!natural)
        {
            ChangeRotation(rotation);
        }
    }

    private void ChangeRotation(float rotation)
    {
        if (rotateTransform) transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        ObjectRotationChangedEvent?.Invoke();
        Shader.SetGlobalFloat(GridRotationController.rotation, rotation);
    }
}
