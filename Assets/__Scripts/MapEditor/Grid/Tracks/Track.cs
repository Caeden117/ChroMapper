using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;

    public Vector3 RotationValue = Vector3.zero;

    public Action TimeChanged;
    private readonly Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    public BaseGrid Object;
    private float spawnPosition;
    private float despawnPosition;

    // this number pulled from my ass, but it looks fine
    const float JUMP_FAR = 500f;

    public void AssignRotationValue(Vector3 rotation)
    {
        RotationValue = rotation;
        transform.RotateAround(rotationPoint, Vector3.right, RotationValue.x);
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue.y);
        transform.RotateAround(rotationPoint, Vector3.forward, RotationValue.z);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y, position);
        TimeChanged?.Invoke();
    }

    public void UpdateTime(float time)
    {
        float z = 0;
        // Jump in
        if (time < Object.SpawnJsonTime)
        {
            z = Mathf.Lerp(spawnPosition, JUMP_FAR, Object.SpawnJsonTime - time);
        }
        else if (time < Object.DespawnJsonTime)
        {
            z = Mathf.Lerp(spawnPosition, despawnPosition, (time - Object.SpawnJsonTime) / (Object.DespawnJsonTime - Object.SpawnJsonTime));
        }
        // Jump out
        else
        {
            z = Mathf.Lerp(despawnPosition, -JUMP_FAR, time - Object.DespawnJsonTime);
        }
        ObjectParentTransform.localPosition = new Vector3(ObjectParentTransform.localPosition.x, ObjectParentTransform.localPosition.y, z);
    }

    public void AttachContainer(ObjectContainer obj)
    {
        UpdateMaterialRotation(obj);
        if (obj.transform.parent == ObjectParentTransform) return;
        obj.transform.SetParent(ObjectParentTransform, false);
        obj.AssignTrack(this);
        if (obj.ObjectData is BaseGrid g) {
            Object = g;
            spawnPosition = Object.Jd;
            despawnPosition = (Object is BaseObstacle obs)
                ? -(Object.Jd * 0.5f) - (obs.Duration * obs.EditorScale)
                : -Object.Jd;
        }
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }
}
