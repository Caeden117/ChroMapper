using System;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;

    public Vector3 RotationValue = Vector3.zero;
    private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    private float oldPosition = 0;

    public Action OnTimeChanged;

    public void AssignRotationValue(Vector3 rotation)
    {
        RotationValue = rotation;
        transform.RotateAround(rotationPoint, Vector3.right, RotationValue.x);
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue.y);
        transform.RotateAround(rotationPoint, Vector3.forward, RotationValue.z);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition += new Vector3(0, 0, position - oldPosition);
        oldPosition = position;

        OnTimeChanged?.Invoke();
    }

    public void AttachContainer(BeatmapObjectContainer obj)
    {
        UpdateMaterialRotation(obj);
        if (obj.transform.parent == ObjectParentTransform) return;
        obj.transform.SetParent(ObjectParentTransform, false);
        obj.AssignTrack(this);
    }

    public void UpdateMaterialRotation(BeatmapObjectContainer obj)
    {
        if (obj is BeatmapObstacleContainer || obj is BeatmapNoteContainer)
        {
            obj.SetRotation(RotationValue.y);
        }
    }
}
