using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    public RotationCallbackController RotationCallback;
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    private float currentRotation;
    private int targetRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private bool isRotating = true;
    public bool IsRotating { get { return isRotating;  } set
        {
            isRotating = value;
            if (value)
                ChangeRotation(targetRotation);
            else transform.localEulerAngles = Vector3.zero;
        } }

    private void Start()
    {
        if (RotationCallback != null) Init();
    }

    public void Init()
    {
        RotationCallback.RotationChangedEvent += RotationChanged;
        if (!GetComponentsInChildren<Renderer>().Any()) return;
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty("_Rotation")));
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        targetRotation = rotation;
        if (!natural)
        {
            ChangeRotation(rotation);
            return;
        }
        StopAllCoroutines();
        if (gameObject.activeSelf) StartCoroutine(ChangeRotationSmooth());
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
        if (!isRotating) return;
        transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        foreach (Renderer g in allRotationalRenderers)
        {
            g.material.SetFloat("_Rotation", transform.eulerAngles.y);
            if (g.material.shader.name.Contains("Grid X"))
                g.material.SetFloat("_Offset", transform.position.x * -1);
        }
    }

    private void OnDestroy()
    {
        RotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
