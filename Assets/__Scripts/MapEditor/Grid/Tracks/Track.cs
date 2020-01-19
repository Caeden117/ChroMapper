using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;

    private List<BeatmapObjectContainer> Containers = new List<BeatmapObjectContainer>();

    public int RotationValue = 0;
    public int RawRotation { get; private set; } = 0;
    private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    public void AssignRotationValue(int rotation, bool rotate = true)
    {
        RotationValue = rotation;
        if (rotate)
        {
            transform.RotateAround(rotationPoint, Vector3.up, RotationValue);
            //transform.localPosition = new Vector3(0, transform.localPosition.y, transform.localPosition.z);
        }
    }

    public void AssignTempRotation(int rotation)
    {
        transform.RotateAround(rotationPoint, Vector3.up, rotation - RotationValue);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y, position);
    }

    public void AttachContainer(BeatmapObjectContainer obj, int rawRotation)
    {
        obj.transform.SetParent(ObjectParentTransform);
        obj.AssignTrack(this);
        Containers.Add(obj);
        if (obj is BeatmapObstacleContainer || obj is BeatmapNoteContainer)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) //Welcome to Python.
                foreach (Material mat in renderer.materials)
                    if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", rawRotation);
        }
        RawRotation = rawRotation;
    }
}
