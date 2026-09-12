using Beatmap.Base;
using UnityEngine;

public class MovementEffect : BasicMovementEffect<MovementStateData>
{
    public Movement Visual;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<Movement>();
    }

    // Finalize builder-assigned dependencies before snapshot construction.
    public override void Initialize()
    {
        if (Visual == null)
            throw new System.InvalidOperationException($"MovementEffect on '{name}' has no Movement visual.");

        Visual.Initialize();
        base.Initialize();
    }

    protected override MovementStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(MovementStateData previous, MovementStateData current)
    {
        var data = Visual.MovementData;
        var len = data.Length;

        if (previous == null)
        {
            current.Offset = data[0];
            current.PreviousOffset = data[0];
            current.Target = data[0];
            current.TargetIndex = 0;
            current.Speed = Visual.TransitionSpeed;
            current.PreviousTarget = data[0];
            current.PreviousSpeed = 0f;
            current.MovingAtSnapshot = false;
            current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
            current.SnapshotFrame = Mathf.FloorToInt(current.SnapshotSeconds / Time.fixedDeltaTime);
            current.AssignmentFrame = int.MinValue;
            current.SameTypeIndex = -1;
            return;
        }

        // Use one global fixed grid across events; restarting the phase at each event shifts interpolation timing.
        current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        current.SnapshotFrame = Mathf.FloorToInt(current.SnapshotSeconds / Time.fixedDeltaTime);
        current.SameTypeIndex = previous.SameTypeIndex + 1;
        var frames = current.SnapshotFrame - previous.SnapshotFrame;
        // Produce both adjacent fixed states in one recurrence; replaying from the same snapshot twice doubled seek cost.
        EvaluateDiscretePair(
            previous,
            frames,
            out current.PreviousOffset,
            out current.Offset,
            out current.MovingAtSnapshot);

        // Beat Saber selects movement data with the carried same-type event index, avoiding an O(n) container scan.
        current.TargetIndex = current.SameTypeIndex % len;
        // Chroma replaces the two-point movement target with a distance along the
        // serialized direction and replaces transition speed when custom data provides it.
        current.Target = len == 2 && current.Base.CustomStep.HasValue
            ? current.Base.CustomStep.Value * (data[1] - data[0]).normalized
            : data[current.TargetIndex];
        current.PreviousTarget = previous.Target;
        current.PreviousSpeed = previous.Speed;
        current.AssignmentFrame = TrackLaneRingsRotationEffect.GetFirstAssignmentFrame(
            current.SnapshotSeconds,
            Time.fixedDeltaTime);
        // Heck gives modern speed precedence and falls back to V2 preciseSpeed.
        current.Speed = current.Base.CustomSpeed ?? current.Base.CustomPreciseSpeed ?? Visual.TransitionSpeed;
    }

    protected override void ApplyVisual(float beat, float seconds, MovementStateData current, MovementStateData next)
    {
        // Render between the previous and current completed fixed states, matching TimeHelper interpolation.
        var framePosition = Atsc.GetSecondsFromBeat(beat) / Time.fixedDeltaTime;
        var fixedFrame = Mathf.FloorToInt(framePosition);
        var frames = fixedFrame - current.SnapshotFrame;
        // Produce both adjacent fixed states in one recurrence; this is the render hot path.
        EvaluateDiscretePair(
            current,
            frames,
            out var previousOffset,
            out var currentOffset,
            out _);
        Visual.Apply(Vector3.LerpUnclamped(previousOffset, currentOffset, framePosition - fixedFrame));
    }

    // Replay MovementBeatmapEventEffect's float LerpUnclamped recurrence and retain both render endpoints.
    private static void EvaluateDiscretePair(
        MovementStateData state,
        int frames,
        out Vector3 previous,
        out Vector3 current,
        out bool movingAtCurrent)
    {
        var value = state.Offset;
        previous = state.PreviousOffset;
        var moving = state.MovingAtSnapshot;
        for (var i = 0; i < frames; i++)
        {
            previous = value;
            var tickFrame = state.SnapshotFrame + i + 1;
            var assigned = tickFrame >= state.AssignmentFrame;
            if (tickFrame == state.AssignmentFrame)
                moving = true;
            if (!moving)
                continue;
            var destination = assigned ? state.Target : state.PreviousTarget;
            var speed = assigned ? state.Speed : state.PreviousSpeed;
            var t = Time.fixedDeltaTime * speed;
            var next = Vector3.LerpUnclamped(value, destination, t);
            // After the delayed assignment has fired, OEM cannot re-enable this state, so later identical ticks can be skipped safely.
            if (next == value || (next - destination).sqrMagnitude < 0.01f)
            {
                value = next;
                if (i < frames - 1)
                    previous = value;
                moving = false;
                // A state that settles before its assignment must keep scanning until that callback can restart movement.
                if (assigned)
                    break;
                continue;
            }
            value = next;
        }
        current = value;
        movingAtCurrent = moving;
    }
}

public class MovementStateData : BasicMovementStateData
{
    public Vector3 Offset;
    public Vector3 PreviousOffset;
    public Vector3 Target;
    public Vector3 PreviousTarget;
    public int TargetIndex;
    public float Speed;
    public float PreviousSpeed;
    public float SnapshotSeconds;
    public int SnapshotFrame;
    public int AssignmentFrame;
    public bool MovingAtSnapshot;

    public MovementStateData(BaseEvent data) : base(data)
    {
    }
}
