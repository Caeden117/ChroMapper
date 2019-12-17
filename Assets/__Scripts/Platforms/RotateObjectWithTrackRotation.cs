using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObjectWithTrackRotation : LightsManager
{
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    private float currentRotation = 0;
    private float targetRotation = 0;

    public void UpdateRotation(float rotation)
    {
        targetRotation = rotation;
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

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    private void ChangeRotation(float rotation)
    {
        transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
    }
}
