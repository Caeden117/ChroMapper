using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

// A pair must consume all three event types in one ordered timeline. Independent managers
// cannot reconstruct switch events or the other side when the playhead is scrubbed backwards.
public class LightPairRotationEffect : BasicMovementEffect<LightPairRotationStateData>
{
    public int LeftEventType;
    public int RightEventType;
    public int SwitchEventType = -1;
    public LightPairRotation Visual;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightPairRotation>();
    }

    // Finalize builder-assigned dependencies before snapshot construction.
    public override void Initialize()
    {
        if (Visual == null)
            throw new System.InvalidOperationException(
                $"LightPairRotationEffect on '{name}' has no LightPairRotation visual.");

        Visual.Initialize();
        base.Initialize();
    }

    protected override LightPairRotationStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightPairRotationStateData previous, LightPairRotationStateData current)
    {
        if (previous == null)
        {
            // The sentinel records both serialized rest angles so evaluating before the first
            // event never depends on whichever transform happened to be rendered previously.
            current.LeftStartAngle = Visual.StartRotation;
            current.RightStartAngle = -current.LeftStartAngle;
            current.LeftAngle = current.LeftStartAngle;
            current.RightAngle = current.RightStartAngle;
            current.LeftSpeed = 0f;
            current.RightSpeed = 0f;
            current.LeftEnabled = false;
            current.RightEnabled = false;
            current.OverrideRandomValues = Visual.OverrideRandomValues;
            current.SwitchEventIndex = 0;
            current.RandomStartRotation = 0f;
            current.RandomDirection = 1f;
            current.HasRandom = false;
            current.CallbackSeconds = 0f;
            return;
        }

        // Basic events dispatch in LateUpdate. Preserve the previous state until the deterministic 90 Hz callback.
        var authoredSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        current.CallbackSeconds = TimeHelper.GetPreviewCallbackSeconds(authoredSeconds);
        // Multiple authored events reached by one callback must all expose the same pre-callback state.
        var sharesCallback = previous.CallbackSeconds == current.CallbackSeconds;
        current.PreviousLeftAngle = sharesCallback ? previous.PreviousLeftAngle : previous.LeftAngle;
        current.PreviousRightAngle = sharesCallback ? previous.PreviousRightAngle : previous.RightAngle;
        current.PreviousLeftSpeed = sharesCallback ? previous.PreviousLeftSpeed : previous.LeftSpeed;
        current.PreviousRightSpeed = sharesCallback ? previous.PreviousRightSpeed : previous.RightSpeed;
        current.PreviousLeftEnabled = sharesCallback ? previous.PreviousLeftEnabled : previous.LeftEnabled;
        current.PreviousRightEnabled = sharesCallback ? previous.PreviousRightEnabled : previous.RightEnabled;
        current.PreviousCallbackSeconds = sharesCallback
            ? previous.PreviousCallbackSeconds
            : previous.CallbackSeconds;
        var deltaSeconds = current.CallbackSeconds - previous.CallbackSeconds;
        current.LeftAngle = GetAngle(previous.LeftAngle, previous.LeftSpeed, previous.LeftEnabled, deltaSeconds);
        current.RightAngle = GetAngle(previous.RightAngle, previous.RightSpeed, previous.RightEnabled, deltaSeconds);
        current.LeftSpeed = previous.LeftSpeed;
        current.RightSpeed = previous.RightSpeed;
        current.LeftStartAngle = previous.LeftStartAngle;
        current.RightStartAngle = previous.RightStartAngle;
        current.LeftEnabled = previous.LeftEnabled;
        current.RightEnabled = previous.RightEnabled;
        current.OverrideRandomValues = previous.OverrideRandomValues;
        current.SwitchEventIndex = previous.SwitchEventIndex;

        if (current.Base.Type == SwitchEventType)
        {
            // Match sameTypeIndex parity: the first callback has index zero, then the
            // retained count advances for the next switch event.
            current.OverrideRandomValues = current.SwitchEventIndex % 2 == 1;
            current.SwitchEventIndex++;
        }

        ResolveRandom(previous, current);

        // OEM dispatch gives left and right precedence when event identifiers overlap with
        // the optional switch identifier, although the switch parity above still advances.
        if (current.Base.Type == LeftEventType)
        {
            ApplyRotationEvent(current, true);
        }
        else if (current.Base.Type == RightEventType)
        {
            ApplyRotationEvent(current, false);
        }
        else if (current.Base.Type == SwitchEventType)
        {
            ApplySwitchEvent(current);
        }
    }

    protected override void ApplyVisual(
        float beat,
        float seconds,
        LightPairRotationStateData current,
        LightPairRotationStateData next)
    {
        // Angles are derived directly from the snapshot and arbitrary song time so pause,
        // rewind, and large seeks never rely on MonoBehaviour.Update or Time.deltaTime.
        var songSeconds = Atsc.GetSecondsFromBeat(beat);
        var beforeCallback = songSeconds < current.CallbackSeconds;
        var leftAngle = beforeCallback
            ? GetAngle(
                current.PreviousLeftAngle,
                current.PreviousLeftSpeed,
                current.PreviousLeftEnabled,
                songSeconds - current.PreviousCallbackSeconds)
            : GetAngle(
                current.LeftAngle,
                current.LeftSpeed,
                current.LeftEnabled,
                songSeconds - current.CallbackSeconds);
        var rightAngle = beforeCallback
            ? GetAngle(
                current.PreviousRightAngle,
                current.PreviousRightSpeed,
                current.PreviousRightEnabled,
                songSeconds - current.PreviousCallbackSeconds)
            : GetAngle(
                current.RightAngle,
                current.RightSpeed,
                current.RightEnabled,
                songSeconds - current.CallbackSeconds);
        Visual.Apply(leftAngle, rightAngle);
    }

    private void ResolveRandom(LightPairRotationStateData previous, LightPairRotationStateData current)
    {
        // Key randomness to the same preview frame used for callback timing.
        var callbackFrame = TimeHelper.GetPreviewRenderIndex(Atsc.GetSecondsFromBeat(current.StartTime));
        if (previous.HasRandom && previous.RandomCallbackFrame == callbackFrame)
        {
            // Beat Saber generates one random pair per callback frame, including distinct
            // authored times crossed by the same deterministic preview callback.
            current.RandomStartRotation = previous.RandomStartRotation;
            current.RandomDirection = previous.RandomDirection;
            current.RandomCallbackFrame = callbackFrame;
            current.HasRandom = true;
            return;
        }

        if (current.HasRandom)
            return;

        if (current.OverrideRandomValues)
        {
            // Retain the callback-frame-derived OEM override once; regenerating it during a
            // dirty-chain recompute would make an unchanged event jump after every edit.
            current.RandomDirection = 1f;
            current.RandomStartRotation = callbackFrame % 360;
            if (Visual.UseZPositionForAngleOffset)
                current.RandomStartRotation += Visual.transform.position.z * Visual.ZPositionAngleOffsetScale;
        }
        else
        {
            current.RandomDirection = Random.value < 0.5f ? 1f : -1f;
            current.RandomStartRotation = Random.Range(0f, 360f);
        }

        current.RandomCallbackFrame = callbackFrame;
        current.HasRandom = true;
    }

    private void ApplyRotationEvent(LightPairRotationStateData state, bool left)
    {
        var value = (float)state.Base.Value;
        var lockRotation = state.Base.CustomLockRotation == true;

        if (value > 0)
        {
            // Heck gives the modern speed key precedence over V2 preciseSpeed.
            if (state.Base.CustomSpeed.HasValue)
                value = state.Base.CustomSpeed.Value;
            else if (state.Base.CustomPreciseSpeed.HasValue)
                value = state.Base.CustomPreciseSpeed.Value;
        }

        var direction = left ? state.RandomDirection : -state.RandomDirection;
        if (state.Base.CustomDirection.HasValue)
        {
            // Chroma mirrors explicit direction by side: dir 0 is negative-left and
            // positive-right, while dir 1 is the inverse.
            if (state.Base.CustomDirection.Value == 0)
                direction = left ? -1f : 1f;
            else
                direction = left ? 1f : -1f;
        }

        if (value == 0)
        {
            // Chroma's lock extension disables motion but deliberately retains the reached
            // angle; without lock, Beat Saber resets only the relevant side to its rest angle.
            if (left)
            {
                state.LeftEnabled = false;
                if (!lockRotation)
                    state.LeftAngle = state.LeftStartAngle;
            }
            else
            {
                state.RightEnabled = false;
                if (!lockRotation)
                    state.RightAngle = state.RightStartAngle;
            }

            return;
        }

        if (value < 0)
            return;

        // Positive events restart only their addressed side at the shared random offset,
        // then use Beat Saber's fixed twenty-degrees-per-song-second value scale.
        if (left)
        {
            state.LeftEnabled = true;
            if (!lockRotation)
                state.LeftAngle = state.RandomStartRotation + state.LeftStartAngle;
            state.LeftSpeed = value * 20f * direction;
        }
        else
        {
            state.RightEnabled = true;
            if (!lockRotation)
                state.RightAngle = -state.RandomStartRotation + state.RightStartAngle;
            state.RightSpeed = value * 20f * direction;
        }
    }

    private static void ApplySwitchEvent(LightPairRotationStateData state)
    {
        // OEM switches reposition both sides even when disabled and normalize their existing
        // speed signs; preserving enabled flags keeps stopped lasers stopped after the switch.
        state.LeftAngle = state.RandomStartRotation + state.LeftStartAngle;
        state.RightAngle = -state.RandomStartRotation + state.RightStartAngle;
        state.LeftSpeed = Mathf.Abs(state.LeftSpeed);
        state.RightSpeed = -Mathf.Abs(state.RightSpeed);
    }

    private static float GetAngle(float angle, float speed, bool enabled, float seconds) =>
        angle + (enabled ? speed * seconds : 0f);
}

// Every node owns a complete pair snapshot because either side or the switch can change the
// future of both transforms, and partial snapshots cannot be safely recomputed after edits.
public class LightPairRotationStateData : BasicMovementStateData
{
    public float LeftAngle;
    public float RightAngle;
    public float LeftSpeed;
    public float RightSpeed;
    public float RandomStartRotation;
    public float RandomDirection;
    public float LeftStartAngle;
    public float RightStartAngle;
    public bool LeftEnabled;
    public bool RightEnabled;
    public bool OverrideRandomValues;
    public bool HasRandom;
    public int SwitchEventIndex;
    public int RandomCallbackFrame;
    public float CallbackSeconds;
    public float PreviousLeftAngle;
    public float PreviousRightAngle;
    public float PreviousLeftSpeed;
    public float PreviousRightSpeed;
    public float PreviousCallbackSeconds;
    public bool PreviousLeftEnabled;
    public bool PreviousRightEnabled;

    public LightPairRotationStateData(BaseEvent data) : base(data)
    {
    }
}
