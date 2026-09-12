using System;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrackLaneRingsRotationEffect : BasicMovementEffect<TrackLaneRingsRotationStateData>
{
    private const int StartupPreRollFrames = 20;
    // Ring recurrence must follow Beat Saber's captured 50 Hz clock, not the editor project's configurable physics setting.
    public const float EmulatedFixedDeltaTime = 0.02f;
    // A 0.7 fixed-step phase preserves the captured separation between dense callbacks;
    // later phases can collapse adjacent events and permanently lose a cumulative target update.
    private const float PreviewPhysicsPhaseFraction = 0.7f;
    // Beat Saber's render lead varies with the run and cannot be reproduced deterministically;
    // zero synthetic lead avoids fixed-pair boundary jitter and matches the accepted editor preview.
    private const float PreviewRenderInterpolationLead = 0f;

    public TrackLaneRingsRotation Visual;

    public float Rotation;
    public float Step;
    public RotationStepType StepType;
    public float PropagationSpeed;
    public float FlexySpeed;

    // Rendering runs every frame, so its scratch state is retained instead of allocating
    // arrays while evaluating the two fixed frames surrounding the playhead.
    // Packing related values cuts each snapshot and retained evaluator from three arrays to one.
    private RingRotationState[] evaluationRingStates;
    private float[] evaluationPreviousRotations;
    private RingRotationWave[] evaluationWaves;
    private TrackLaneRingsRotationStateData evaluationState;
    private int evaluationFrame;
    private int evaluationWaveCount;
    private bool evaluationValid;
    private bool isPlaying;
    private bool allowsCounterSpin;
    // Unity overloads null comparison; cache the initialized manager so snapshot reconstruction does not pay that native check per effect and event.
    private TrackLaneRingsManager ringManager;
    // The declaration is immutable for an open map, so resolve it once instead of parsing three JSON scopes for every event snapshot.
    private bool mappedForOldPropagation;
    // GreenDayGrenadeInactiveRingRotationEffectInitializes covers the inactive OEM template that intentionally owns no rings until enhancement setup.
    private bool isDormantTemplate;
    private float appliedSongSeconds = float.NaN;

    // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions loads GreenDayGrenade's inactive serialized
    // ring effect through BasicEventEffectManager, so establish its dependency in the invoked lifecycle before snapshots.
    public override void Initialize()
    {
        if (Visual == null)
            Visual = GetComponent<TrackLaneRingsRotation>();

        if (Visual != null)
        {
            // Chroma counter-spins every named ring system except spawners containing "Big".
            allowsCounterSpin = !name.Contains("Big");
            Visual.enabled = false;
        }

        // A missing serialized lifecycle dependency must fail at initialization instead
        // of becoming a delayed null failure inside snapshot or render evaluation.
        if (Visual == null || Visual.Manager == null)
            throw new InvalidOperationException($"Ring rotation '{name}' has no initialized ring manager.");

        // Initialize establishes the scene-lifetime manager dependency before the first snapshot; snapshot and
        // render hot paths can then use the cached manager without recovery lookups.
        ringManager = Visual.Manager;
        ringManager.Atsc = Atsc;
        ringManager.UseCached = true;

        var songContainer = BeatSaberSongContainer.Instance;
        mappedForOldPropagation = songContainer != null
            && RingPropagationCompatibility.IsMappedForOldPropagation(
                songContainer.MapDifficultyInfo?.CustomData,
                songContainer.Info?.CustomData,
                songContainer.Map?.CustomData);
        // GreenDayGrenadeInactiveRingRotationEffectInitializes distinguishes its inactive, empty template from a live effect that lost its rings.
        isDormantTemplate = !gameObject.activeInHierarchy && ringManager.Rings.Count == 0;

        base.Initialize();
    }

    protected override TrackLaneRingsRotationStateData CreateState(BaseEvent data) => new(data);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        this.isPlaying = isPlaying;

        // Both play and pause must use one deterministic state evaluator. Switching
        // to the old live queue on resume cannot reconstruct overlapping propagation.

        base.UpdateTime(isPlaying, currentTime);
    }

    protected override void ComputeSnapshot(TrackLaneRingsRotationStateData previous, TrackLaneRingsRotationStateData current)
    {
        // An edit can recompute an existing state object in place, so invalidate any
        // retained live evaluator that was derived from its old snapshot contents.
        if (evaluationState == current)
        {
            evaluationValid = false;
            appliedSongSeconds = float.NaN;
        }

        var rings = ringManager.Rings;
        var ringCount = rings.Count;
        // GreenDayGrenadeInactiveRingRotationEffectInitializes keeps its inactive empty template valid; zero-ring effects have no rotation state to compute.
        if (ringCount == 0)
        {
            if (isDormantTemplate)
            {
                current.RingStates = Array.Empty<RingRotationState>();
                current.ActiveWaves = Array.Empty<RingRotationWave>();
                current.ActiveWaveCount = 0;
            }

            return;
        }

        EnsureSnapshotArrays(current, ringCount, previous != null ? previous.ActiveWaveCount + 1 : 1);

        if (previous == null)
        {
            for (var i = 0; i < ringCount; i++)
            {
                var rotation = rings[i].GetRotation();
                current.RingStates[i] = new RingRotationState
                {
                    Rotation = rotation,
                    Destination = rotation,
                    Speed = 0f
                };
            }

            current.ActiveWaveCount = 0;
            current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
            current.SnapshotFrame = -1;
            current.TargetResolutionFrame = -StartupPreRollFrames;
            current.AssignmentFrame = current.TargetResolutionFrame + 1;
            current.FirstRingDest = Visual.StartupRotationAngle;
            current.Step = Visual.StartupRotationStep;
            current.Rotation = Visual.StartupRotationAngle;
            current.Propagation = Visual.StartupRotationPropagationSpeed;
            current.Speed = Visual.StartupRotationFlexySpeed;
            current.Clockwise = false;
            current.CounterSpin = false;
            current.HasRandom = false;

            // Beat Saber constructs this wave before song playback. Use the 90 Hz capture's
            // twenty-tick pre-roll as the deterministic editor convention.
            AddWave(current, true);
            AdvanceState(
                current.RingStates,
                current.ActiveWaves,
                ref current.ActiveWaveCount,
                -StartupPreRollFrames,
                current.SnapshotFrame,
                ringCount);
            return;
        }

        // Only unfinished wave cursors are copied; expanding their future ring work here
        // made event insertion proportional to every assignment that had not happened yet.
        for (var i = 0; i < ringCount; i++)
            current.RingStates[i] = previous.RingStates[i];

        current.ActiveWaveCount = previous.ActiveWaveCount;
        for (var i = 0; i < current.ActiveWaveCount; i++)
            current.ActiveWaves[i] = previous.ActiveWaves[i];

        current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        // The snapshot must precede the event's phased render pair. Using the unphased
        // floor put snapshots at the pair's current endpoint during the first 40% of a
        // fixed interval, so a rebuild integrated that endpoint twice and visibly jumped
        // older waves on far rings before the new propagation could reach them.
        current.SnapshotFrame = GetPreviewSnapshotFrame(
            current.SnapshotSeconds,
            EmulatedFixedDeltaTime);
        current.AssignmentFrame = GetFirstAssignmentFrame(current.SnapshotSeconds, EmulatedFixedDeltaTime);
        // Resolve cumulative targets immediately before the callback-containing preview
        // state so assignments already exposed there affect later callback groups.
        current.TargetResolutionFrame = current.AssignmentFrame - 1;
        AdvanceState(
            current.RingStates,
            current.ActiveWaves,
            ref current.ActiveWaveCount,
            previous.SnapshotFrame,
            current.SnapshotFrame,
            ringCount);

        current.Rotation = current.Base.CustomRingRotation ?? Rotation;

        // Snapshot recomputes must reuse the originally selected random values or edits
        // elsewhere in the chain would make unchanged authored events drift.
        if (!current.HasRandomStep)
        {
            current.RandomStep = GetRandomStep();
            current.HasRandomStep = true;
        }

        // Chroma rolls step before its case-insensitive filter, but direction only for matching spawners.
        if (!string.IsNullOrEmpty(current.Base.CustomNameFilter)
            && !string.Equals(current.Base.CustomNameFilter, name, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!current.HasRandom)
        {
            current.Clockwise = Random.value >= 0.5f;
            current.CounterSpin = current.Base.CustomData != null
                && (current.Base.CustomData.GetValueOrDefault("_counterSpin", null)?.AsBool ?? false);
            current.HasRandom = true;
        }

        // Start from the retained random choice on every recompute so a step multiplier
        // cannot compound when an earlier edited event invalidates this snapshot.
        current.Step = current.RandomStep;
        if (current.Base.CustomStep.HasValue)
            current.Step = current.Base.CustomStep.Value;
        if (current.Base.CustomStepMult.HasValue)
            current.Step *= current.Base.CustomStepMult.Value;

        if (current.Base.CustomDirection.HasValue)
            current.Clockwise = current.Base.CustomDirection.Value == 0;

        var hasExplicitPropagation = current.Base.CustomProp.HasValue;
        var prop = current.Base.CustomProp ?? PropagationSpeed;
        // BeatToTheFuture changes explicit prop/_prop before Chroma applies propMult, so mirror that order and leave environment defaults untouched.
        prop = RingPropagationCompatibility.ApplyVisualMultiplier(
            prop,
            hasExplicitPropagation,
            mappedForOldPropagation);
        if (current.Base.CustomPropMult.HasValue)
            prop *= current.Base.CustomPropMult.Value;
        // Chroma propagates with the authored float exactly. Non-positive values never
        // advance its active wave, so they intentionally produce no assignments here.
        current.Propagation = prop;

        // Heck gives modern speed precedence and falls back to V2 preciseSpeed.
        current.Speed = current.Base.CustomSpeed ?? current.Base.CustomPreciseSpeed ?? FlexySpeed;
        if (current.Base.CustomSpeedMult.HasValue)
            current.Speed *= current.Base.CustomSpeedMult.Value;

        // Chroma's reset branch ignores custom rotation parameters and emits the
        // environment's base rotation with its hard-coded immediate wave settings.
        var reset = current.Base.CustomData != null
            && (current.Base.CustomData.GetValueOrDefault("_reset", null)?.AsBool ?? false);
        if (reset)
        {
            current.Rotation = Rotation;
            current.Step = 0f;
            current.Propagation = 50f;
            current.Speed = 50f;
        }

        var signed = current.Rotation * (current.Clockwise ? 1f : -1f);
        if (allowsCounterSpin && current.CounterSpin)
            signed *= -1f;

        // Resolve the first-ring destination at the callback frame, not the authored event
        // time. Older waves can assign between those times at low or uneven render rates.
        current.RotationDelta = signed;
        // Seed the wave's initial target; callback-frame resolution refreshes it from authoritative ring state.
        current.FirstRingDest = current.RingStates[0].Destination + signed;

        // Keep the float cursor itself: truncating it afresh each tick preserves Chroma's
        // repeated fractional assignments without pre-expanding the rest of the wave.
        AddWave(current, false);
    }

    protected override void ApplyVisual(float beat, float seconds, TrackLaneRingsRotationStateData current, TrackLaneRingsRotationStateData next)
    {
        var rings = ringManager.Rings;
        var ringCount = rings.Count;
        // Snapshot construction owns state validity, so the render hot path retains only reusable evaluator storage.
        EnsureEvaluationArrays(ringCount, current.ActiveWaveCount);

        // Beat Saber renders the latest phased fixed pair while TimeHelper may extrapolate
        // it. Evaluate the exact requested song time so paused 1/64 stepping does not jump
        // forward to a later synthetic render; callback scheduling remains on its 90 Hz grid.
        var songSeconds = current.SnapshotSeconds + seconds;
        var fixedDeltaTime = EmulatedFixedDeltaTime;
        GetPreviewRenderState(
            songSeconds,
            fixedDeltaTime,
            out _,
            out var frame,
            out var interpolation);
        if (isPlaying
            && songSeconds == appliedSongSeconds
            && evaluationValid
            && evaluationState == current)
        {
            return;
        }

        var previousFrame = frame - 1;

        // Only continuous playback may reuse mutable evaluator cursors. Paused stepping,
        // rewind, and seeks always rebuild from the immutable snapshot so the result cannot
        // depend on which direction or sequence of editor navigation reached this time.
        var canAdvanceIncrementally = isPlaying
            && evaluationValid
            && evaluationState == current
            && frame >= evaluationFrame
            && frame <= evaluationFrame + 1;
        if (!canAdvanceIncrementally)
        {
            evaluationValid = false;
            for (var i = 0; i < ringCount; i++)
                evaluationRingStates[i] = current.RingStates[i];

            evaluationWaveCount = current.ActiveWaveCount;
            for (var i = 0; i < evaluationWaveCount; i++)
                evaluationWaves[i] = current.ActiveWaves[i];

            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                current.SnapshotFrame,
                previousFrame,
                ringCount);
            for (var i = 0; i < ringCount; i++)
                evaluationPreviousRotations[i] = evaluationRingStates[i].Rotation;

            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                previousFrame,
                frame,
                ringCount);

            evaluationState = current;
            evaluationFrame = frame;
            evaluationValid = true;
        }
        else if (frame > evaluationFrame)
        {
            // The prior current state is exactly the next render frame's previous state.
            for (var i = 0; i < ringCount; i++)
                evaluationPreviousRotations[i] = evaluationRingStates[i].Rotation;

            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                evaluationFrame,
                frame,
                ringCount);
            evaluationFrame = frame;
        }

        appliedSongSeconds = songSeconds;
        for (var i = 0; i < ringCount; i++)
        {
            // TrackLaneRing uses the raw TimeHelper interpolation expression rather than a clamped Mathf.Lerp.
            var rotation = evaluationPreviousRotations[i]
                + ((evaluationRingStates[i].Rotation - evaluationPreviousRotations[i]) * interpolation);
            // localEulerAngles immediately converts through Quaternion.Euler before calling
            // localRotation; perform that conversion directly to remove one native wrapper
            // from each of the thousands of ring transform writes shown in the profiler.
            rings[i].CachedTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
    }

    private static void EnsureSnapshotArrays(TrackLaneRingsRotationStateData state, int ringCount, int waveCapacity)
    {
        if (state.RingStates == null || state.RingStates.Length != ringCount)
            state.RingStates = new RingRotationState[ringCount];
        if (state.ActiveWaves == null || state.ActiveWaves.Length < waveCapacity)
            state.ActiveWaves = new RingRotationWave[waveCapacity];
    }

    private void EnsureEvaluationArrays(int ringCount, int waveCapacity)
    {
        if (evaluationRingStates == null || evaluationRingStates.Length != ringCount)
        {
            evaluationValid = false;
            evaluationRingStates = new RingRotationState[ringCount];
            evaluationPreviousRotations = new float[ringCount];
        }

        // This grows only with overlapping waves and is reused by every rendered frame.
        if (evaluationWaves == null || evaluationWaves.Length < waveCapacity)
            evaluationWaves = new RingRotationWave[waveCapacity];
    }

    // Assignments happen before their fixed tick's lerp. Wave creation order is retained,
    // then traversed backwards because Chroma processes active effects newest-to-oldest.
    // Public so the captured Beat Saber regression fixture exercises the exact production
    // wave evaluator rather than a duplicate test-only implementation.
    public static void AdvanceState(
        RingRotationState[] ringStates,
        RingRotationWave[] waves,
        ref int waveCount,
        int fromFrame,
        int toFrame,
        int ringCount)
    {
        if (toFrame <= fromFrame)
            return;

        // During ordinary forward playback every surviving wave is already resolved and
        // assigns on this exact tick. Snapshot reconstruction can contain future or overdue
        // waves, which must retain the generic catch-up path to preserve propagation order.
        if (toFrame == fromFrame + 1
            && CanAdvanceSingleFrame(waves, waveCount, toFrame))
        {
            AdvanceSingleFrame(
                ringStates,
                waves,
                ref waveCount,
                ringCount);
            return;
        }

        ResolveWaveTargets(ringStates, waves, waveCount, fromFrame);
        var frame = fromFrame;
        while (waveCount > 0)
        {
            var actionFrame = int.MaxValue;
            for (var i = 0; i < waveCount; i++)
            {
                actionFrame = Mathf.Min(
                    actionFrame,
                    waves[i].Created ? waves[i].NextFrame : waves[i].CreationFrame);
            }

            if (actionFrame > toFrame)
                break;

            var hasAssignment = false;
            for (var i = 0; i < waveCount; i++)
            {
                if (waves[i].Created && waves[i].NextFrame == actionFrame)
                {
                    hasAssignment = true;
                    break;
                }
            }

            // A callback at actionFrame occurs after that frame's fixed update. If an
            // assignment also occurs there, integrate it first, then resolve new targets.
            LerpAll(
                ringStates,
                actionFrame - frame - (hasAssignment ? 1 : 0),
                ringCount);
            for (var i = waveCount - 1; i >= 0; i--)
            {
                var wave = waves[i];
                if (!wave.Created || wave.NextFrame != actionFrame)
                    continue;

                // Chroma truncates the old float ProgressPos before advancing it, so a
                // fractional wave can deliberately assign the same ring on many ticks.
                var ring = (long)wave.Progress;
                wave.Progress += wave.Propagation;
                while (ring < wave.Progress && ring < ringCount)
                {
                    var ringIndex = (int)ring;
                    var target = wave.FirstRingDestination + (ring * wave.Step);
                    ringStates[ringIndex].Destination = target;
                    ringStates[ringIndex].Speed = wave.Speed;
                    ring++;
                }

                wave.NextFrame++;
                waves[i] = wave;
            }

            RemoveCompletedWaves(waves, ref waveCount, ringCount);
            if (hasAssignment)
                LerpAll(ringStates, 1, ringCount);

            frame = actionFrame;
            ResolveWaveTargets(ringStates, waves, waveCount, frame);
        }

        LerpAll(ringStates, toFrame - frame, ringCount);
    }

    // Keep the hot path strictly to the invariant established by a prior generic step.
    // Any unresolved, future, or overdue cursor falls through to the authoritative evaluator.
    private static bool CanAdvanceSingleFrame(RingRotationWave[] waves, int waveCount, int assignmentFrame)
    {
        for (var i = 0; i < waveCount; i++)
        {
            if (!waves[i].Created || waves[i].NextFrame != assignmentFrame)
            {
                return false;
            }
        }

        return true;
    }

    // The common fixed-frame path combines the generic evaluator's target scan, assignment
    // scan, completion scan, and final target scan into one assignment pass without changing
    // its newest-to-oldest overwrite order or per-ring Mathf.Lerp recurrence.
    private static void AdvanceSingleFrame(
        RingRotationState[] ringStates,
        RingRotationWave[] waves,
        ref int waveCount,
        int ringCount)
    {
        for (var i = waveCount - 1; i >= 0; i--)
        {
            var wave = waves[i];
            var ring = (long)wave.Progress;
            wave.Progress += wave.Propagation;
            while (ring < wave.Progress && ring < ringCount)
            {
                var ringIndex = (int)ring;
                var target = wave.FirstRingDestination + (ring * wave.Step);
                ringStates[ringIndex].Destination = target;
                ringStates[ringIndex].Speed = wave.Speed;
                ring++;
            }

            wave.NextFrame++;
            waves[i] = wave;
        }

        RemoveCompletedWaves(waves, ref waveCount, ringCount);
        LerpAll(ringStates, 1, ringCount);
    }

    private static void ResolveWaveTargets(
        RingRotationState[] ringStates,
        RingRotationWave[] waves,
        int waveCount,
        int frame)
    {
        for (var i = 0; i < waveCount; i++)
        {
            var wave = waves[i];
            if (wave.Created || wave.CreationFrame > frame)
                continue;

            // All events dispatched by one callback observe the same pre-assignment ring
            // destination; merely creating an earlier wave does not change that destination.
            wave.FirstRingDestination = ringStates[0].Destination + wave.RotationDelta;
            wave.NextFrame = wave.CreationFrame + 1;
            wave.Created = true;
            waves[i] = wave;
        }
    }

    private static void RemoveCompletedWaves(RingRotationWave[] waves, ref int waveCount, int ringCount)
    {
        var destination = 0;
        for (var source = 0; source < waveCount; source++)
        {
            if (waves[source].Created && !(waves[source].Progress < ringCount))
                continue;

            waves[destination++] = waves[source];
        }

        waveCount = destination;
    }

    private static void LerpAll(RingRotationState[] ringStates, int frames, int ringCount)
    {
        if (frames <= 0)
            return;

        var fixedDeltaTime = EmulatedFixedDeltaTime;
        for (var i = 0; i < ringCount; i++)
        {
            var ringState = ringStates[i];
            // Most cloned rings are already at their destination between waves. Mathf.Lerp
            // would clamp and return the same value before the existing break notices that,
            // so skip that identity recurrence without changing any fixed-tick state.
            if (ringState.Rotation == ringState.Destination)
            {
                continue;
            }

            var interpolation = fixedDeltaTime * ringState.Speed;
            // Mathf.Lerp clamps non-positive factors to zero and factors at or above one to
            // the destination. Handle those exact outcomes directly to avoid clamp calls on
            // high-speed and disabled ring effects during full-map snapshot reconstruction.
            if (interpolation <= 0f)
            {
                continue;
            }

            if (interpolation >= 1f)
            {
                ringState.Rotation = ringState.Destination;
                ringStates[i] = ringState;
                continue;
            }

            for (var frame = 0; frame < frames; frame++)
            {
                // The factor is proven strictly inside Mathf.Lerp's clamp range above,
                // so LerpUnclamped is the identical recurrence without a Clamp01 call.
                var rotation = Mathf.LerpUnclamped(
                    ringState.Rotation,
                    ringState.Destination,
                    interpolation);
                if (rotation == ringState.Rotation)
                    break;

                ringState.Rotation = rotation;
            }

            ringStates[i] = ringState;
        }
    }

    private void AddWave(TrackLaneRingsRotationStateData state, bool startup)
    {
        if (state.Propagation <= 0f)
            return;

        state.ActiveWaves[state.ActiveWaveCount++] = new RingRotationWave
        {
            CreationFrame = state.TargetResolutionFrame,
            NextFrame = state.AssignmentFrame,
            Progress = 0f,
            FirstRingDestination = state.FirstRingDest,
            RotationDelta = state.RotationDelta,
            Step = state.Step,
            Propagation = state.Propagation,
            Speed = state.Speed,
            Created = startup
        };
    }

    public static int GetFirstAssignmentFrame(float songSeconds, float fixedDeltaTime)
    {
        // Chroma creates waves in a render callback, then its IFixedTickable processes them
        // on the following physics tick. Applying them in the callback-containing state makes
        // stacked fractional waves accumulate an extra tick of rotation and speed overrides.
        // BeatmapCallbacksController dispatches when eventTime <= songTime, so an event
        // exactly on the synthetic render grid belongs to that callback, not the next one.
        var callbackSeconds = TimeHelper.GetPreviewCallbackSeconds(songSeconds);
        // The deterministic 90 Hz preview uses the effective phase that best preserves the
        // captured ordering between render callbacks and their following physics ticks.
        var physicsPhase = fixedDeltaTime * PreviewPhysicsPhaseFraction;
        return Mathf.FloorToInt((callbackSeconds - physicsPhase) / fixedDeltaTime) + 1;
    }

    public static void GetPreviewRenderState(
        float songSeconds,
        float fixedDeltaTime,
        out int renderIndex,
        out int fixedFrame,
        out float interpolation)
    {
        renderIndex = TimeHelper.GetPreviewRenderIndex(songSeconds);
        // Double arithmetic keeps the fixed position stable at long song times instead of
        // accumulating float additions once per preview frame.
        var unphasedFixedPosition = songSeconds / (double)fixedDeltaTime;
        // The retained startup snapshot is fixed frame -1; frame zero is the first pair
        // it can reconstruct without inventing a pre-snapshot rotation endpoint.
        fixedFrame = Math.Max(0, (int)Math.Floor(unphasedFixedPosition));

        // Use the unphased song-time fraction without Beat Saber's run-specific render lead;
        // adding the captured lead here caused discontinuities whenever the fixed pair advanced.
        interpolation = (float)((unphasedFixedPosition - fixedFrame) + PreviewRenderInterpolationLead);
    }

    public static int GetPreviewSnapshotFrame(float songSeconds, float fixedDeltaTime)
    {
        // Event snapshots are immutable rewind points, so they must own the state before
        // both endpoints that ApplyVisual reconstructs at the exact authored event time.
        GetPreviewRenderState(
            songSeconds,
            fixedDeltaTime,
            out _,
            out var fixedFrame,
            out _);
        return fixedFrame - 1;
    }

    private float GetRandomStep() => StepType switch
    {
        RotationStepType.Range0ToMax => Random.Range(0f, Step),
        RotationStepType.Range => Random.Range(0f - Step, Step),
        RotationStepType.MaxOr0 => Random.value < 0.5f ? Step : 0f,
        _ => 0f
    };
}

public struct RingRotationState
{
    public float Rotation;
    public float Destination;
    public float Speed;
}

public struct RingRotationWave
{
    public int CreationFrame;
    public int NextFrame;
    public float Progress;
    public float FirstRingDestination;
    public float RotationDelta;
    public float Step;
    public float Propagation;
    public float Speed;
    public bool Created;
}

public class TrackLaneRingsRotationStateData : BasicMovementStateData
{
    public RingRotationState[] RingStates;
    public RingRotationWave[] ActiveWaves;
    public int ActiveWaveCount;
    public int SnapshotFrame;
    public int TargetResolutionFrame;
    public int AssignmentFrame;
    public float SnapshotSeconds;
    public float FirstRingDest;
    public float RotationDelta;
    public float RandomStep;
    public float Step;
    public float Rotation;
    public float Propagation;
    public float Speed;
    public bool Clockwise;
    public bool CounterSpin;
    public bool HasRandom;
    public bool HasRandomStep;

    public TrackLaneRingsRotationStateData(BaseEvent data) : base(data)
    {
    }
}

public enum RotationStepType : byte
{
    Range0ToMax,
    Range,
    MaxOr0
}
