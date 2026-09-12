using Beatmap.Base;
using UnityEngine;

public class TrackLaneRingsPositionEffect : BasicMovementEffect<TrackLaneRingsPositionStateData>
{
    public TrackLaneRingsPositionSpawner Visual;

    // Paste and undo re-evaluate this state chain immediately, so record the missing
    // snapshot dependency at construction instead of leaving only a later hot-path null.
    private bool reportedUnavailableRingSnapshot;
    // GreenDayGrenadeInactiveRingRotationEffectInitializes covers its inactive Event 9 spawner, whose Awake cannot establish the serialized reverse binding.
    private bool isDormantTemplate;
    private TrackLaneRingsManager ringManager;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<TrackLaneRingsPositionSpawner>();

        if (Visual != null && Visual.RingManager != null)
            Visual.RingManager.UseCached = true;
    }

    protected override TrackLaneRingsPositionStateData CreateState(BaseEvent data) => new(data);

    public override void Initialize()
    {
        // Missing serialized bindings are asset errors, not runtime recovery cases.
        if (Visual == null || Visual.RingManager == null)
            throw new System.InvalidOperationException($"Ring position '{name}' has no initialized ring manager.");

        // Runtime-built components may receive Visual after Awake.
        ringManager = Visual.RingManager;
        ringManager.Atsc = Atsc;
        ringManager.UseCached = true;
        // GreenDayGrenadeInactiveRingRotationEffectInitializes distinguishes its inactive empty template from a wired live effect whose rings disappeared.
        isDormantTemplate = !Visual.gameObject.activeInHierarchy && ringManager.Rings.Count == 0;

        base.Initialize();
    }

    protected override void ComputeSnapshot(TrackLaneRingsPositionStateData previous, TrackLaneRingsPositionStateData current)
    {
        var rings = ringManager.Rings;
        var ringCount = rings.Count;
        if (ringCount == 0)
        {
            // GreenDayGrenadeInactiveRingRotationEffectInitializes permits the inactive empty template to stay
            // dormant, while a wired effect with no rings still reports the reproducible paste/undo lifecycle gap.
            if (!isDormantTemplate && !reportedUnavailableRingSnapshot)
            {
                var previousPositions = previous?.RingPositions?.Length ?? -1;
                var previousFrames = previous?.PreviousRingPositions?.Length ?? -1;
                Debug.LogError(
                    $"Ring position '{name}' cannot build beat {current.StartTime:R}: "
                    + $"Visual={Visual.name}, Manager={ringManager.name}, rings={ringCount}, "
                    + $"previousPositions={previousPositions}, previousFrames={previousFrames}.",
                    this);
                reportedUnavailableRingSnapshot = true;
            }

            return;
        }

        reportedUnavailableRingSnapshot = false;
        if (current.RingPositions == null
            || current.PreviousRingPositions == null
            || current.RingPositions.Length != ringCount
            || current.PreviousRingPositions.Length != ringCount)
        {
            current.RingPositions = new float[ringCount];
            current.PreviousRingPositions = new float[ringCount];
        }

        if (previous == null)
        {
            // Start sentinel: capture the initial ring Z positions.
            for (var i = 0; i < ringCount; i++)
            {
                current.RingPositions[i] = rings[i].PositionZ;
                current.PreviousRingPositions[i] = current.RingPositions[i];
            }

            current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
            // Frame -1 is the captured pre-song state; frame zero is the first fixed pair
            // shared with rotation once the audio controller begins rendering.
            current.SnapshotFrame = -1;
            current.SameTypeIndex = -1;
            current.Step = 0f;
            current.Speed = 0f;
            current.PreviousStep = 0f;
            current.PreviousSpeed = 0f;
            current.AssignmentFrame = int.MinValue;
            return;
        }

        current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        // Position uses the same pre-render-pair snapshot invariant as rotation; otherwise
        // entering an early-phase zoom event can integrate its current endpoint twice.
        current.AssignmentFrame = TrackLaneRingsRotationEffect.GetFirstAssignmentFrame(
            current.SnapshotSeconds,
            TrackLaneRingsRotationEffect.EmulatedFixedDeltaTime);
        current.SnapshotFrame = TrackLaneRingsRotationEffect.GetPreviewSnapshotFrame(
            current.SnapshotSeconds,
            TrackLaneRingsRotationEffect.EmulatedFixedDeltaTime);
        current.SameTypeIndex = previous.SameTypeIndex + 1;
        var frames = current.SnapshotFrame - previous.SnapshotFrame;
        for (var i = 0; i < ringCount; i++)
        {
            // Replay the prior event's delayed LateUpdate assignment while carrying the snapshot chain forward.
            EvaluateDiscretePair(
                previous,
                i,
                frames,
                out current.PreviousRingPositions[i],
                out current.RingPositions[i]);
        }

        current.PreviousStep = previous.Step;
        current.PreviousSpeed = previous.Speed;
        // Heck gives modern speed precedence and falls back to V2 preciseSpeed.
        current.Speed = current.Base.CustomSpeed ?? current.Base.CustomPreciseSpeed ?? Visual.MoveSpeed;
        // Chroma's ring-step patch supports precise step and speed only; legacy multipliers do not apply here.
        current.Step = current.Base.CustomStep
            ?? (current.SameTypeIndex % 2 == 0 ? Visual.MaxPositionStep : Visual.MinPositionStep);
    }

    protected override void ApplyVisual(float beat, float seconds, TrackLaneRingsPositionStateData current, TrackLaneRingsPositionStateData next)
    {
        // Position and rotation are fields of the same OEM TrackLaneRing and therefore
        // must use the same phased fixed pair and unclamped TimeHelper render factor.
        TrackLaneRingsRotationEffect.GetPreviewRenderState(
            current.SnapshotSeconds + seconds,
            TrackLaneRingsRotationEffect.EmulatedFixedDeltaTime,
            out _,
            out var fixedFrame,
            out var interpolation);
        var frames = fixedFrame - current.SnapshotFrame;
        var rings = ringManager.Rings;
        for (var i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            // Replay both sides of the modeled LateUpdate assignment on the render hot path.
            EvaluateDiscretePair(
                current,
                i,
                frames,
                out var previousPosition,
                out var currentPosition);
            var position = previousPosition + ((currentPosition - previousPosition) * interpolation);
            ring.CachedTransform.localPosition = new Vector3(
                ring.PositionOffset.x,
                ring.PositionOffset.y,
                position);
        }
    }

    // Replay Unity's float recurrence exactly and retain both render endpoints in one pass.
    private void EvaluateDiscretePair(
        TrackLaneRingsPositionStateData state,
        int ringIndex,
        int frames,
        out float previous,
        out float current)
    {
        var value = state.RingPositions[ringIndex];
        previous = state.PreviousRingPositions[ringIndex];
        var positionOffset = ringManager.Rings[ringIndex].PositionOffset.z;
        for (var i = 0; i < frames; i++)
        {
            previous = value;
            var tickFrame = state.SnapshotFrame + i + 1;
            var assigned = tickFrame >= state.AssignmentFrame;
            var step = assigned ? state.Step : state.PreviousStep;
            var speed = assigned ? state.Speed : state.PreviousSpeed;
            var destination = positionOffset + (ringIndex * step);
            // Keep zoom on the same captured Beat Saber fixed clock as ring rotation instead of inheriting the editor physics setting.
            var next = Mathf.Lerp(value, destination, TrackLaneRingsRotationEffect.EmulatedFixedDeltaTime * speed);
            // A stable old destination cannot skip a later callback assignment in this interval.
            if (next == value && assigned)
                break;
            value = next;
        }
        current = value;
    }
}

public class TrackLaneRingsPositionStateData : BasicMovementStateData
{
    public float[] RingPositions;
    public float[] PreviousRingPositions;
    public float Step;
    public float Speed;
    public float PreviousStep;
    public float PreviousSpeed;
    public float SnapshotSeconds;
    public int SnapshotFrame;
    public int AssignmentFrame;

    public TrackLaneRingsPositionStateData(BaseEvent data) : base(data)
    {
    }
}
