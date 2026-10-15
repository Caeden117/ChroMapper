using System;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform SelfTransform;
    public Transform ObjectParentTransform;
    public VariableNJSProvider vNjsProvider;
    public bool IgnoreZScale;

    public Vector3 RotationValue = Vector3.zero;

    private readonly Vector3 rotationPoint = Vector3.zero;

    private BaseGrid gridObject;
    private ObjectContainer gridContainer;
    private (Transform, Vector3, Quaternion)[] nodesTarget;
    private readonly Vector3[] newSplinePosition = new Vector3[ArcContainer.NumSamples + 1];
    private bool useCustom;
    private float spawnTime;
    private float spawnPosition;
    private float despawnTime;
    private float despawnPosition;
    private float zScale = 1f;
    private bool v2;

    // this number pulled from my ass, but it looks fine
    // oh, it's actually correct
    const float JUMP_FAR = 500f;

    // this number also pulled from my ass, song bpm time
    public const float JUMP_TIME = 2f;

    public void Awake() => zScale = IgnoreZScale ? 1f : BeatmapConstant.LaneSize;

    public void OnEnable() => vNjsProvider.OnChanged += UpdateState;
    public void OnDisable() => vNjsProvider.OnChanged -= UpdateState;

    public void AssignRotationValue(Vector3 rotation)
    {
        RotationValue = rotation;
        SelfTransform.RotateAround(rotationPoint, Vector3.right, RotationValue.x);
        SelfTransform.RotateAround(rotationPoint, Vector3.up, RotationValue.y);
        SelfTransform.RotateAround(rotationPoint, Vector3.forward, RotationValue.z);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(
            ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y,
            position * zScale);
    }

    // Definite-position notes must stay at their resting lane height while fading in. Clear travel Z and pin
    // parent Y so UpdateTime cannot add the jump arc. Other grid objects retain their Y position.
    public void HoldDefinitePosition()
    {
        var position = ObjectParentTransform.localPosition;
        var y = gridObject is BaseNote note
            ? note.GetPosition().y + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset
            : position.y;
        ObjectParentTransform.localPosition = new Vector3(position.x, y, 0f);
    }

    public void UpdateTime(float time)
    {
        float z;
        var position = ObjectParentTransform.localPosition;

        // Jump in
        if (time < spawnTime)
        {
            z = (gridObject.CustomSpawnEffect != null ? (bool)gridObject.CustomSpawnEffect : !v2) ^ v2
                ? Mathf.LerpUnclamped(spawnPosition, JUMP_FAR, (spawnTime - time) / JUMP_TIME)
                : JUMP_FAR;
        }
        else if (time < despawnTime)
            z = Mathf.LerpUnclamped(spawnPosition, despawnPosition, (time - spawnTime) / (despawnTime - spawnTime));
        // Jump out
        else
            z = Mathf.LerpUnclamped(despawnPosition, -JUMP_FAR, (time - despawnTime) / JUMP_TIME);

        position.z = z;

        // oh yeah you know its good when things start with a check like this
        switch (gridObject)
        {
            case BaseNote note:
                {
                    // Normalized [0-1] between despawn time and spawn time
                    var normalizedLifetime = Mathf.Clamp01(Mathf.InverseLerp(despawnTime, spawnTime, time));

                    // [0-1] between spawn time and note time
                    // 0.3 magic number taken from ArcViewer (thanks polandball)
                    var spawnLifetime = Mathf.Clamp01(1 - ((normalizedLifetime - 0.5f) * 2));
                    var rotationLifetime = Mathf.Clamp01(spawnLifetime / 0.3f);

                    // Beat Saber uses a parabolic arc so we use Quadratic Out easing because im lazy
                    var jumpT = Easing.Quadratic.Out(spawnLifetime);
                    var rotationT = Easing.Quadratic.Out(rotationLifetime);

                    // TODO: Pre-compute starting position so notes can stack and flip can be supported
                    //   (Notes need to be aware of other notes)
                    position.y = Mathf.LerpUnclamped(
                        BeatmapConstant.YOffset,
                        note.GetPosition().y + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset,
                        jumpT);

                    // Multiply euler rotation by spawn lifetime if we are in the first half (spawning) portion of our object lifetime
                    if (normalizedLifetime >= 0.5f && gridContainer is NoteContainer noteContainer)
                    {
                        var quaternion = Quaternion.Euler(noteContainer.DirectionTargetEuler);

                        noteContainer.DirectionTarget.localRotation = Quaternion.LerpUnclamped(
                            Quaternion.identity,
                            quaternion,
                            rotationT);
                    }

                    break;
                }
            case BaseChain chain:
                {
                    var len = Math.Min(nodesTarget.Length, chain.SliceCount - 1);
                    for (var i = 0; i < len; i++)
                    {
                        var (nodeTransform, targetPosition, targetRotation) = nodesTarget[i];
                        var localPosition = nodeTransform.localPosition;
                        var offset = Mathf.LerpUnclamped(
                            0f,
                            chain.TailSongBpmTime - chain.SongBpmTime,
                            (i + 1f) / (chain.SliceCount - 1f));

                        // it's just copypasted above
                        var normalizedLifetime = Mathf.Clamp01(
                            Mathf.InverseLerp(
                                despawnTime + offset,
                                spawnTime + offset,
                                time));
                        var spawnLifetime = Mathf.Clamp01(1 - ((normalizedLifetime - 0.5f) * 2));
                        var rotationLifetime = Mathf.Clamp01(spawnLifetime / 0.3f);
                        var jumpT = Easing.Quadratic.Out(spawnLifetime);
                        var rotationT = Easing.Quadratic.Out(rotationLifetime);
                        localPosition.y = Mathf.LerpUnclamped(
                            -position.y
                            + BeatmapConstant.YOffset,
                            targetPosition.y,
                            jumpT);
                        nodeTransform.localPosition = localPosition;
                        nodeTransform.localRotation = Quaternion.LerpUnclamped(
                            Quaternion.identity,
                            targetRotation,
                            rotationT);
                    }

                    break;
                }
            case BaseArc arc:
                {
                    var arcContainer = gridContainer as ArcContainer;

                    var normalizedLifetime = Mathf.Clamp01(
                        Mathf.InverseLerp(
                            arc.SongBpmTime + arc.HalfJumpDuration,
                            arc.SpawnSongBpmTime,
                            time));
                    var spawnLifetime = Mathf.Clamp01(1f - ((normalizedLifetime - 0.5f) * 2f));
                    var jumpT = arcContainer.ArcData.HeadNotes.Count > 0 ? Easing.Quadratic.Out(spawnLifetime) : 1f;
                    var headPosY = arc.GetPosition().y;
                    var headY = Mathf.LerpUnclamped(-BeatmapConstant.PlayerYOffset - headPosY, 0f, jumpT);

                    var tailOffset = arc.DurationSongBpmTime;
                    var tailNormalizedLifetime = Mathf.Clamp01(
                        Mathf.InverseLerp(
                            arc.SongBpmTime + arc.HalfJumpDuration + tailOffset,
                            arc.SpawnSongBpmTime + tailOffset,
                            time));
                    var tailSpawnLifetime = Mathf.Clamp01(1f - ((tailNormalizedLifetime - 0.5f) * 2f));
                    var tailJumpT = arcContainer.ArcData.TailNotes.Count > 0
                        ? Easing.Quadratic.Out(tailSpawnLifetime)
                        : 1f;
                    var tailPosY = arc.GetTailPosition().y;
                    var tailY = Mathf.LerpUnclamped(-BeatmapConstant.PlayerYOffset - tailPosY, 0f, tailJumpT);

                    // yoink from polandball
                    // https://github.com/AllPoland/ArcViewer/blob/main/Assets/__Scripts/Previewer/MapControl/Objects/ArcManager.cs#L362
                    var basePositions = arcContainer.BaseSplinePoints;
                    var arcLength = basePositions[^1].z;
                    for (var i = 0; i < basePositions.Length; i++)
                    {
                        var point = basePositions[i];

                        //Get the preferred offset based on distance from the head
                        var headDist = point.z / arc.HalfJumpDistance;
                        var headT = 1 - Easing.Quadratic.Out(Mathf.Clamp01(headDist));
                        var headPreferredOffset = headY * headT;

                        //Get the preferred offset based on distance from the tail
                        var tailDist = (arcLength - point.z) / arc.HalfJumpDistance;
                        var tailT = 1 - Easing.Quadratic.Out(Mathf.Clamp01(tailDist));
                        var tailPreferredOffset = tailY * tailT;

                        //Weight the adjustment based on which end of the arc the point is closer to
                        var relativePosition = point.z / arcLength;
                        point.y += Mathf.LerpUnclamped(headPreferredOffset, tailPreferredOffset, relativePosition);

                        //Squish the arc if needed
                        point.z *= arc.DurationSongBpmTime * arc.HalfJumpDistance / arc.HalfJumpDuration;

                        newSplinePosition[i] = point;
                    }

                    var splineRenderer = arcContainer.SplineRenderer;
                    splineRenderer.SetPositions(newSplinePosition);
                    break;
                }
        }

        ObjectParentTransform.localPosition = position;
    }

    public void InitState()
    {
        v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
        useCustom = (gridObject.CustomNoteJumpMovementSpeed?.IsNumber ?? false)
            || (gridObject.CustomNoteJumpStartBeatOffset?.IsNumber ?? false);
        if (!useCustom)
        {
            gridObject.SetSpawnParameters(
                vNjsProvider.HalfJumpDurationInBeats,
                vNjsProvider.HalfJumpDistance);
        }

        UpdateSpawning();
    }

    public void UpdateState()
    {
        if (!UIMode.PreviewMode || useCustom || gridObject == null || gridContainer.ObjectData == null) return;

        gridObject.SetSpawnParameters(
            vNjsProvider.HalfJumpDurationInBeats,
            vNjsProvider.HalfJumpDistance);
        UpdateSpawning();
    }

    public void UpdateSpawning()
    {
        spawnTime = gridObject.SongBpmTime - gridObject.HalfJumpDuration;
        spawnPosition = gridObject.HalfJumpDistance;
        switch (gridObject)
        {
            case BaseObstacle obs:
                despawnPosition = -(obs.HalfJumpDistance * 0.5f)
                    - (obs.DurationSongBpmTime * obs.HalfJumpDistance / obs.HalfJumpDuration);
                despawnTime = obs.SongBpmTime + obs.DurationSongBpmTime + (obs.HalfJumpDuration * 0.5f);
                break;
            case BaseArc arc:
                despawnPosition = -arc.HalfJumpDistance
                    - (arc.DurationSongBpmTime * arc.HalfJumpDistance / arc.HalfJumpDuration);
                despawnTime = arc.DespawnSongBpmTime;
                break;
            case BaseChain chain:
                despawnPosition = -chain.HalfJumpDistance
                    - (chain.DurationSongBpmTime * chain.HalfJumpDistance / chain.HalfJumpDuration);
                despawnTime = chain.DespawnSongBpmTime;
                break;
            default:
                despawnPosition = -gridObject.HalfJumpDistance;
                despawnTime = gridObject.DespawnSongBpmTime;
                break;
        }

        spawnPosition += BeatmapConstant.ZOffset;
        despawnPosition += BeatmapConstant.ZOffset;

        gridContainer.UpdateScalable(gridObject.HalfJumpDistance / gridObject.HalfJumpDuration);
    }

    public void AttachContainer(ObjectContainer obj)
    {
        UpdateMaterialRotation(obj);
        if (obj.transform.parent == ObjectParentTransform) return;
        obj.transform.SetParent(ObjectParentTransform, false);
        obj.AssignTrack(this);

        if (obj.ObjectData is not BaseGrid g) return;
        gridContainer = obj;
        gridObject = g;

        if (gridContainer is ChainContainer chainContainer)
        {
            nodesTarget = chainContainer
                .Nodes.Select(n => (n.transform, n.transform.localPosition, n.transform.localRotation))
                .ToArray();
        }

        InitState();
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }

    public void ResetData()
    {
        enabled = false;
        SelfTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        SelfTransform.localScale = Vector3.one;
        ObjectParentTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ObjectParentTransform.localScale = Vector3.one;

        if (gameObject.TryGetComponent<TrackAnimator>(out var animator)) animator.enabled = false;
    }
}
