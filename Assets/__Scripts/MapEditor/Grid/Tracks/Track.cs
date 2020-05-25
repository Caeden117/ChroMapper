using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;

    public float RotationValue = 0;
    private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    private float oldPosition = 0;

    public void AssignRotationValue(float rotation)
    {
        RotationValue = rotation;
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue);
    }
    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition += new Vector3(0, 0, position - oldPosition);
        oldPosition = position;
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
            foreach (Material mat in obj.ModelMaterials)
                if (mat.HasProperty("_Rotation"))
                    mat.SetFloat("_Rotation", RotationValue);
        }
    }
}
