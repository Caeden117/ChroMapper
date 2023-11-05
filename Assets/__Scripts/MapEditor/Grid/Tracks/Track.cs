using System;
using Beatmap.Base;
using Beatmap.V2;
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
    private float despawnTime;

    // this number pulled from my ass, but it looks fine
    // oh, it's actually correct
    const float JUMP_FAR = 500f;

    // this number also pulled from my ass and used inconsistently as json time and bpm time, oh well!
    public const float JUMP_TIME = 2f;

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
        bool v2 = Object is V2Object;
        // Jump in
        if (time < Object.SpawnJsonTime)
        {
            z = ((Object.CustomSpawnEffect ?? !v2) ^ v2) ? Mathf.Lerp(spawnPosition, JUMP_FAR, (Object.SpawnJsonTime - time) / JUMP_TIME) : JUMP_FAR;
        }
        else if (time < despawnTime)
        {
            z = Mathf.Lerp(spawnPosition, despawnPosition, (time - Object.SpawnJsonTime) / (despawnTime - Object.SpawnJsonTime));
        }
        // Jump out
        else
        {
            z = Mathf.Lerp(despawnPosition, -JUMP_FAR, (time - despawnTime) / JUMP_TIME);
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
            if (Object is BaseObstacle obs)
            {
                despawnPosition = -(Object.Jd * 0.5f) - (obs.Duration * obs.EditorScale);
                despawnTime = obs.JsonTime + obs.Duration + (obs.Hjd * 0.5f);
            }
            else
            {
                despawnPosition = -Object.Jd;
                despawnTime = Object.DespawnJsonTime;
            }
        }
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }
}
