﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    public RotationCallbackController RotationCallback;
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    [SerializeField] private bool rotateTransform = true;

    public Action ObjectRotationChangedEvent;

    private float currentRotation;
    private int targetRotation;
    private int cachedRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private static readonly int Offset = Shader.PropertyToID("_Offset");

    private void Start()
    {
        if (RotationCallback != null) Init();
    }

    public void Init()
    {
        RotationCallback.RotationChangedEvent += RotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
        if (!GetComponentsInChildren<Renderer>().Any()) return;
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty("_Rotation")));
    }

    private void UpdateRotateTrack(object obj)
    {
        bool rotating = (bool)obj;
        if (rotating)
        {
            targetRotation = RotationCallback.Rotation;
            ChangeRotation(RotationCallback.Rotation);
        }
        else
        {
            targetRotation = 0;
            ChangeRotation(0);
        }
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        cachedRotation = rotation;
        if (!natural)
        {
            targetRotation = rotation;
            ChangeRotation(rotation);
            return;
        }
    }

    private void Update()
    {
        if (RotationCallback is null || !RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        if (targetRotation != cachedRotation)
        {
            targetRotation = cachedRotation;
            StopAllCoroutines();
            if (gameObject.activeInHierarchy) StartCoroutine(ChangeRotationSmooth());
        }
    }

    private IEnumerator ChangeRotationSmooth()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / rotationChangingTime;
            ChangeRotation(Mathf.Lerp(currentRotation, targetRotation, t));
            yield return new WaitForEndOfFrame();
        }
    }

    private void ChangeRotation(float rotation)
    {
        if (rotateTransform) transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        ObjectRotationChangedEvent?.Invoke();
        foreach (Renderer g in allRotationalRenderers)
        {
            g.material.SetFloat(Rotation, transform.eulerAngles.y);
            if (g.material.shader.name.Contains("Grid X"))
                g.material.SetFloat(Offset, transform.position.x * (rotateTransform ? -1 : 1));
        }
    }

    private void OnDestroy()
    {
        RotationCallback.RotationChangedEvent -= RotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }
}
