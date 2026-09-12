using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightRotationEffect : BasicMovementEffect<LightRotationStateData>
{
    public LightRotation Visual;
    public float SpeedMultiplier = 1f;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightRotation>();
    }

    public override void Initialize()
    {
        // Finalize builder-assigned dependencies before snapshot construction.
        if (Visual == null)
            throw new System.InvalidOperationException(
                $"LightRotationEffect on '{name}' has no LightRotation visual.");

        Visual.Initialize();
        base.Initialize();
    }

    protected override LightRotationStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightRotationStateData previous, LightRotationStateData current)
    {
        var evt = current.Base;
        var eventValue = evt.Value;
        var resolvedSpeed = (float)eventValue;

        if (previous == null)
        {
            // Start sentinel: t=0 has no rotation.
            current.Angle = 0f;
            current.Enabled = false;
            current.Speed = 0f;
            current.StartOffset = 0f;
            current.Direction = 0f;
            current.HasRandom = false;
            current.Lock = false;
            current.CallbackSeconds = 0f;
            return;
        }

        // Basic events dispatch in LateUpdate. Use the deterministic 90 Hz preview callback convention so speed changes
        // do not integrate during the authored-time-to-callback gap.
        var authoredSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        current.CallbackSeconds = TimeHelper.GetPreviewCallbackSeconds(authoredSeconds);
        // Multiple authored events reached by one callback must all expose the same pre-callback state.
        var sharesCallback = previous.CallbackSeconds == current.CallbackSeconds;
        current.PreviousAngle = sharesCallback ? previous.PreviousAngle : previous.Angle;
        current.PreviousSpeed = sharesCallback ? previous.PreviousSpeed : previous.Speed;
        current.PreviousEnabled = sharesCallback ? previous.PreviousEnabled : previous.Enabled;
        current.PreviousCallbackSeconds = sharesCallback
            ? previous.PreviousCallbackSeconds
            : previous.CallbackSeconds;
        var deltaSeconds = current.CallbackSeconds - previous.CallbackSeconds;
        var preEventAngle = previous.Angle + (previous.Enabled ? previous.Speed * deltaSeconds : 0f);

        var lockRotation = evt.CustomLockRotation == true;

        if (eventValue > 0)
        {
            // Chroma uses speed as the motion amount but still uses the Basic Event value to select stop/start behavior.
            if (evt.CustomSpeed.HasValue)
                resolvedSpeed = evt.CustomSpeed.Value;
            else if (evt.CustomPreciseSpeed.HasValue)
                resolvedSpeed = evt.CustomPreciseSpeed.Value;
        }

        if (!current.HasRandom && evt.CustomData != null)
        {
            // Chroma resolves direction before its value switch, including stop/ignored
            // events, and rolls the positive-event offset afterward only when unlocked.
            if (!evt.CustomDirection.HasValue)
                current.Direction = Random.value > 0.5f ? 1f : -1f;
            if (eventValue > 0 && !lockRotation)
                current.StartOffset = Random.Range(0f, 180f);
            current.HasRandom = true;
        }
        else if (!current.HasRandom && eventValue > 0)
        {
            // The stock effect rolls its offset before its direction boolean.
            current.StartOffset = Random.Range(0f, 180f);
            current.Direction = Random.value < 0.5f ? 1f : -1f;
            current.HasRandom = true;
        }

        if (evt.CustomDirection.HasValue)
        {
            // Chroma defines direction relative to the laser side: event 12 mirrors
            // the same custom direction value used by the opposite rotating laser.
            var isLeftEvent = evt.Type == 12;
            current.Direction = evt.CustomDirection.Value == 0
                ? isLeftEvent ? -1f : 1f
                : isLeftEvent ? 1f : -1f;
        }

        current.Lock = lockRotation;

        if (eventValue == 0)
        {
            current.Enabled = false;
            current.Speed = 0f;
            current.Angle = lockRotation ? preEventAngle : 0f;
        }
        else if (eventValue > 0)
        {
            current.Enabled = true;
            current.Angle = lockRotation ? preEventAngle : current.StartOffset;
            // Keep serialized speed on the state manager: snapshots can be computed before
            // a dynamically built visual is available, while Chroma custom data still bypasses it.
            var speedMultiplier = evt.CustomData != null ? 1f : SpeedMultiplier;
            current.Speed = resolvedSpeed * speedMultiplier * 20f * current.Direction;
        }
        else
        {
            // Beat Saber and Chroma ignore negative Basic Event values instead of treating them as enabled speeds.
            current.Enabled = previous.Enabled;
            current.Speed = previous.Speed;
            current.Angle = preEventAngle;
        }
    }

    protected override void ApplyVisual(float beat, float seconds, LightRotationStateData current, LightRotationStateData next)
    {
        // Before LateUpdate dispatch, continue the previous event; after it, integrate the newly applied speed.
        var songSeconds = Atsc.GetSecondsFromBeat(beat);
        var beforeCallback = songSeconds < current.CallbackSeconds;
        var angle = beforeCallback
            ? current.PreviousAngle
                + (current.PreviousEnabled
                    ? current.PreviousSpeed * (songSeconds - current.PreviousCallbackSeconds)
                    : 0f)
            : current.Angle
                + (current.Enabled ? current.Speed * (songSeconds - current.CallbackSeconds) : 0f);
        Visual.Apply(angle);
    }
}

public class LightRotationStateData : BasicMovementStateData
{
    public float StartOffset;   // random 0..180 applied on an enabled event
    public float Direction;     // -1 or 1
    public float Speed;
    public float Angle;         // the actual angle at this node's start
    public bool Enabled;
    public bool Lock;           // Chroma: do not reset the transform on this event
    public bool HasRandom;      // have StartOffset/Direction already been resolved for this node
    public float CallbackSeconds;
    public float PreviousAngle;
    public float PreviousSpeed;
    public float PreviousCallbackSeconds;
    public bool PreviousEnabled;

    public LightRotationStateData(BaseEvent data) : base(data)
    {
    }
}
