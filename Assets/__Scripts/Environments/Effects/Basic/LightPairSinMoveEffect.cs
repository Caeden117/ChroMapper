using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairSinMoveEffect : BasicMovementEffect<LightPairSinMoveStateData>
{
    public int LeftEventType = -1;
    public int RightEventType = -1;
    public int SwitchEventType = -1;
    public LightPairSinMove Visual;

    // A timeline rebuild must reuse the phase chosen for a callback frame; otherwise an unrelated edit makes lasers jump.
    private readonly Dictionary<int, float> randomPhaseByFrame = new();
    private readonly Dictionary<BaseEvent, float> switchRandomPhaseByEvent = new();

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightPairSinMove>();
    }

    public override void Initialize()
    {
        // Finalize builder-assigned dependencies before snapshot construction.
        if (Visual == null)
            throw new System.InvalidOperationException(
                $"LightPairSinMoveEffect on '{name}' has no LightPairSinMove visual.");

        Visual.Initialize();
        // Reinitialization replaces the event timeline, so retained event keys and
        // callback-frame random phases must not leak into the next map state.
        randomPhaseByFrame.Clear();
        switchRandomPhaseByEvent.Clear();
        base.Initialize();
    }

    protected override LightPairSinMoveStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightPairSinMoveStateData previous, LightPairSinMoveStateData current)
    {
        if (previous == null)
        {
            // The sentinel describes both lasers before any event and preserves the environment's serialized override.
            var startValueOffset = Visual.StartValueOffset;
            current.LeftPhase = startValueOffset;
            current.RightPhase = startValueOffset;
            current.LeftSpeed = 0f;
            current.RightSpeed = 0f;
            current.LeftEnabled = false;
            current.RightEnabled = false;
            current.OverrideRandomValues = Visual.OverrideRandomValues;
            current.SwitchEventCount = 0;
            current.RandomPhaseFrame = int.MinValue;
            current.CallbackSeconds = 0f;
            return;
        }

        // Basic events dispatch in LateUpdate. Preserve the previous phase until the deterministic 90 Hz callback.
        var authoredSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        current.CallbackSeconds = TimeHelper.GetPreviewCallbackSeconds(authoredSeconds);
        // Multiple authored events reached by one callback must all expose the same pre-callback state.
        var sharesCallback = previous.CallbackSeconds == current.CallbackSeconds;
        current.PreviousLeftPhase = sharesCallback ? previous.PreviousLeftPhase : previous.LeftPhase;
        current.PreviousRightPhase = sharesCallback ? previous.PreviousRightPhase : previous.RightPhase;
        current.PreviousLeftSpeed = sharesCallback ? previous.PreviousLeftSpeed : previous.LeftSpeed;
        current.PreviousRightSpeed = sharesCallback ? previous.PreviousRightSpeed : previous.RightSpeed;
        current.PreviousLeftEnabled = sharesCallback ? previous.PreviousLeftEnabled : previous.LeftEnabled;
        current.PreviousRightEnabled = sharesCallback ? previous.PreviousRightEnabled : previous.RightEnabled;
        current.PreviousCallbackSeconds = sharesCallback
            ? previous.PreviousCallbackSeconds
            : previous.CallbackSeconds;
        var deltaSeconds = current.CallbackSeconds - previous.CallbackSeconds;
        current.LeftPhase = EvaluatePhase(previous.LeftPhase, previous.LeftSpeed, previous.LeftEnabled, deltaSeconds);
        current.RightPhase = EvaluatePhase(previous.RightPhase, previous.RightSpeed, previous.RightEnabled, deltaSeconds);
        current.LeftSpeed = previous.LeftSpeed;
        current.RightSpeed = previous.RightSpeed;
        current.LeftEnabled = previous.LeftEnabled;
        current.RightEnabled = previous.RightEnabled;
        current.OverrideRandomValues = previous.OverrideRandomValues;
        current.SwitchEventCount = previous.SwitchEventCount;
        current.RandomPhase = previous.RandomPhase;
        current.RandomPhaseFrame = previous.RandomPhaseFrame;
        // Key randomness to the same preview frame used for callback timing.
        var callbackFrame = TimeHelper.GetPreviewRenderIndex(Atsc.GetSecondsFromBeat(current.StartTime));

        var evt = current.Base;
        if (evt.Type == SwitchEventType)
        {
            // Beat Saber derives the switch from same-type index parity and deliberately rerolls that callback frame.
            current.OverrideRandomValues = (current.SwitchEventCount % 2) == 1;
            current.SwitchEventCount++;
            current.RandomPhase = GetSwitchRandomPhase(evt, current.OverrideRandomValues);
            current.RandomPhaseFrame = callbackFrame;
            var startValueOffset = Visual.StartValueOffset;
            current.LeftPhase = current.RandomPhase + startValueOffset;
            current.RightPhase = current.RandomPhase + startValueOffset;
            current.LeftSpeed = Mathf.Abs(current.LeftSpeed);
            // Preserve the game's switch behavior: the right speed is copied from the now-positive left speed.
            current.RightSpeed = Mathf.Abs(current.LeftSpeed);
            return;
        }

        // Same-time callbacks share the frame's phase, including a phase rerolled by a preceding switch callback.
        if (current.RandomPhaseFrame != callbackFrame)
        {
            current.RandomPhase = GetRandomPhase(callbackFrame, current.OverrideRandomValues);
            current.RandomPhaseFrame = callbackFrame;
        }

        if (evt.Type == LeftEventType)
            ApplyEvent(evt.Value, current.RandomPhase, ref current.LeftPhase, ref current.LeftSpeed, ref current.LeftEnabled);
        else if (evt.Type == RightEventType)
            ApplyEvent(evt.Value, -current.RandomPhase, ref current.RightPhase, ref current.RightSpeed, ref current.RightEnabled);
    }

    protected override void ApplyVisual(float beat, float seconds, LightPairSinMoveStateData current, LightPairSinMoveStateData next)
    {
        // Continue the previous state through the callback gap, then integrate the newly applied event.
        var songSeconds = Atsc.GetSecondsFromBeat(beat);
        var beforeCallback = songSeconds < current.CallbackSeconds;
        var leftPhase = beforeCallback
            ? EvaluatePhase(
                current.PreviousLeftPhase,
                current.PreviousLeftSpeed,
                current.PreviousLeftEnabled,
                songSeconds - current.PreviousCallbackSeconds)
            : EvaluatePhase(
                current.LeftPhase,
                current.LeftSpeed,
                current.LeftEnabled,
                songSeconds - current.CallbackSeconds);
        var rightPhase = beforeCallback
            ? EvaluatePhase(
                current.PreviousRightPhase,
                current.PreviousRightSpeed,
                current.PreviousRightEnabled,
                songSeconds - current.PreviousCallbackSeconds)
            : EvaluatePhase(
                current.RightPhase,
                current.RightSpeed,
                current.RightEnabled,
                songSeconds - current.CallbackSeconds);
        Visual.Apply(leftPhase, rightPhase);
    }

    private float GetRandomPhase(int callbackFrame, bool overrideRandomValues)
    {
        if (overrideRandomValues)
            return 0f;

        // Events at one timeline time represent one callback frame and therefore share one random phase.
        if (!randomPhaseByFrame.TryGetValue(callbackFrame, out var phase))
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            randomPhaseByFrame.Add(callbackFrame, phase);
        }

        return phase;
    }

    private float GetSwitchRandomPhase(BaseEvent evt, bool overrideRandomValues)
    {
        if (overrideRandomValues)
            return 0f;

        // A switch invalidates the shared frame phase in the game, but its replacement must remain stable on recompute.
        if (!switchRandomPhaseByEvent.TryGetValue(evt, out var phase))
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            switchRandomPhaseByEvent.Add(evt, phase);
        }

        return phase;
    }

    private void ApplyEvent(int value, float phaseOffset, ref float phase, ref float speed, ref bool enabled)
    {
        var startValueOffset = Visual.StartValueOffset;
        if (value == 0)
        {
            // A zero event resets position but retains speed because a later switch reads the stored game speed.
            enabled = false;
            phase = startValueOffset;
        }
        else if (value > 0)
        {
            enabled = true;
            phase = phaseOffset + startValueOffset;
            speed = value * 1f;
        }
    }

    private static float EvaluatePhase(float phase, float speed, bool enabled, float seconds) =>
        phase + (enabled ? speed * seconds : 0f);
}

public class LightPairSinMoveStateData : BasicMovementStateData
{
    public float LeftPhase;
    public float RightPhase;
    public float LeftSpeed;
    public float RightSpeed;
    public bool LeftEnabled;
    public bool RightEnabled;
    public bool OverrideRandomValues;
    public int SwitchEventCount;
    public float RandomPhase;
    public int RandomPhaseFrame;
    public float CallbackSeconds;
    public float PreviousLeftPhase;
    public float PreviousRightPhase;
    public float PreviousLeftSpeed;
    public float PreviousRightSpeed;
    public float PreviousCallbackSeconds;
    public bool PreviousLeftEnabled;
    public bool PreviousRightEnabled;

    public LightPairSinMoveStateData(BaseEvent data) : base(data)
    {
    }
}
