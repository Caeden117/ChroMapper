using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    private float currentRotation;
    private int targetRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private void Start()
    {
        rotationCallback.RotationChangedEvent += RotationChanged;
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty("_Rotation")));
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!rotationCallback.IsActive) return;
        targetRotation = rotation;
        if (!natural)
        {
            ChangeRotation(rotation);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(ChangeRotationSmooth());
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
        rotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
