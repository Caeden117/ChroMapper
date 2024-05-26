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

    // this number also pulled from my ass, song bpm time
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
        var z = 0f;
        var v2 = Object is V2Object;
        var position = ObjectParentTransform.localPosition;

        // Jump in
        if (time < Object.SpawnSongBpmTime)
        {
            z = ((Object.CustomSpawnEffect ?? !v2) ^ v2) ? Mathf.Lerp(spawnPosition, JUMP_FAR, (Object.SpawnSongBpmTime - time) / JUMP_TIME) : JUMP_FAR;
        }
        else if (time < despawnTime)
        {
            z = Mathf.Lerp(spawnPosition, despawnPosition, (time - Object.SpawnSongBpmTime) / (despawnTime - Object.SpawnSongBpmTime));
        }
        // Jump out
        else
        {
            z = Mathf.Lerp(despawnPosition, -JUMP_FAR, (time - despawnTime) / JUMP_TIME);
        }

        position.z = z;

        // oh yeah you know its good when things start with a check like this
        if (Object is BaseNote note)
        {
            // Normalized [0-1] between despawn time and spawn time
            var normalizedLifetime = Mathf.Clamp01(Mathf.InverseLerp(Object.DespawnSongBpmTime, Object.SpawnSongBpmTime, time));
            
            // [0-1] between spawn time and note time
            // 0.3 magic number taken from ArcViewer (thanks polandball)
            var spawnLifetime = Mathf.Clamp01(1 - ((normalizedLifetime - 0.5f) * 2));
            var rotationLifetime = Mathf.Clamp01(spawnLifetime / 0.3f);

            // Beat Saber uses a parabolic arc so we use Quadratic Out easing because im lazy
            var jumpT = Easing.Quadratic.Out(spawnLifetime);
            var rotationT = Easing.Quadratic.Out(rotationLifetime);

            // Magic 1.1 number comes from ObjectContainer.offsetY which is currently protected
            // TODO: Pre-compute starting position so notes can stack and flip can be supported
            //   (Notes need to be aware of other notes)
            position.y = Mathf.Lerp(1.1f, note.GetPosition().y + 1.1f, jumpT);

            // Multiply euler rotation by spawn lifetime if we are in the first half (spawning) portion of our object lifetime
            if (normalizedLifetime >= 0.5f)
            {
                // OK this is hacky i sincerely apologize
                var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(note.ObjectType);
                
                if (containerCollection.LoadedContainers.TryGetValue(Object, out var container) && container is NoteContainer noteContainer)
                {
                    // TODO: Pre-compute final rotation so note snapping is retained
                    //   (Notes need to be aware of other notes)
                    var euler = NoteContainer.Directionalize(note);
                    var quaternion = Quaternion.Euler(euler);

                    noteContainer.DirectionTarget.localRotation = Quaternion.Lerp(Quaternion.identity, quaternion, rotationT);
                }
            }

        }

        ObjectParentTransform.localPosition = position;
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
                despawnPosition = -(Object.Jd * 0.5f) - (obs.DurationSongBpm * obs.EditorScale);
                despawnTime = obs.SongBpmTime + obs.DurationSongBpm + (obs.Hjd * 0.5f);
            }
            else
            {
                despawnPosition = -Object.Jd;
                despawnTime = Object.DespawnSongBpmTime;
            }
        }
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }
}
