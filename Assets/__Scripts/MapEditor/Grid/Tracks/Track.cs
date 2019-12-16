using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    [SerializeField] private Transform objectParentTransform;

    private List<BeatmapObjectContainer> Containers = new List<BeatmapObjectContainer>();

    public int RotationValue = 0;
    private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    public void AssignRotationValue(int rotation, bool rotate = true)
    {
        RotationValue = rotation;
        if (rotate) transform.RotateAround(rotationPoint, Vector3.up, RotationValue);
    }

    public void UpdatePosition(float position)
    {
        objectParentTransform.localPosition = new Vector3(transform.position.x, transform.position.y, position);
    }

    public void AttachContainer(BeatmapObjectContainer obj)
    {
        obj.transform.SetParent(objectParentTransform, true);
        Containers.Add(obj);
    }
}
