using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;

    public int RotationValue = 0;
    private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    public void AssignRotationValue(int rotation)
    {
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue * -1);
        RotationValue = rotation;
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y, position);
    }

    public void AttachContainer(BeatmapObjectContainer obj)
    {
        obj.transform.SetParent(ObjectParentTransform, false);
        obj.AssignTrack(this);
        if (obj is BeatmapObstacleContainer || obj is BeatmapNoteContainer)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) //Welcome to Python.
                foreach (Material mat in renderer.materials)
                    if (mat.HasProperty("_Rotation")) mat.SetFloat("_Rotation", RotationValue);
        }
    }
}
