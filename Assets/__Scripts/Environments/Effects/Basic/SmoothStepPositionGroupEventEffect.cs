using System.Linq;
using Beatmap.Base;
using UnityEngine;

// TheSecondRingZoom* reproduces Beat Saber's group-spacing Event 9 effect while accepting Chroma's fractional step.
public class SmoothStepPositionGroupEventEffect : BasicMovementEffect<SmoothStepPositionGroupStateData>
{
    public Transform[] Elements;
    public bool ClampValue;
    public float MinValue;
    public float MaxValue;
    public Vector3 BaseOffset;
    public Vector3 MovementVector = Vector3.forward;
    public float StepSize;

    private Vector3[] initialPositions;
    private readonly Vector3Tween tween = new();

    private void Awake()
    {
        // The fallback Z replaces the missing serialized value. Adding it as an offset would move rings twice when EnvironmentData already supplies Z.
        BaseOffset.z = 1f;

        // The Second uses a plain ordered child group rather than TrackLaneRingsManager/TrackLaneRing components.
        var children = new Transform[transform.childCount];
        initialPositions = new Vector3[transform.childCount];
        for (var i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
            initialPositions[i] = children[i].localPosition;
        }

        // Sort The Second's rings along the movement axis; their hierarchy order does not match their depth.
        var elementOrder = Enumerable.Range(0, children.Length)
            .OrderBy(i => Vector3.Dot(initialPositions[i], MovementVector))
            .ToArray();
        Elements = elementOrder.Select(i => children[i]).ToArray();
        initialPositions = elementOrder.Select(i => initialPositions[i]).ToArray();

        tween.Easing = Easing.Cubic.InOut;
    }

    protected override SmoothStepPositionGroupStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(
        SmoothStepPositionGroupStateData previous,
        SmoothStepPositionGroupStateData current)
    {
        // Chroma steps bypass the OEM integer clamp.
        var hasCustomStep = current.Base.CustomStep.HasValue;
        var value = hasCustomStep
            ? current.Base.CustomStep.Value
            : current.Base.Value;
        current.Position = IsStartSentinel(current)
            ? Vector3.zero
            : GetPositionForValue(value, !hasCustomStep);
    }

    protected override void ApplyVisual(
        float beat,
        float seconds,
        SmoothStepPositionGroupStateData current,
        SmoothStepPositionGroupStateData next)
    {
        // The start sentinel represents the untouched environment before Event 9 first fires, not a tween toward that event.
        if (IsStartSentinel(current))
        {
            RestoreInitialPositions();
            return;
        }

        if (next == null)
        {
            SetPosition(current.Position);
            return;
        }

        tween.StartValue = current.Position;
        tween.EndValue = next.Position;
        tween.StartTime = current.StartTime;
        tween.EndTime = next.StartTime;
        tween.UpdateTime(beat);
        SetPosition(tween.Current);
    }

    // The negative-step regressions keep i inside OEM bounds and reserve the extended signed domain for customData.step.
    private Vector3 GetPositionForValue(float value, bool applyIntegerClamp)
    {
        if (ClampValue && applyIntegerClamp)
        {
            value = Mathf.Clamp(value, MinValue, MaxValue);
        }

        return BaseOffset + (MovementVector * (StepSize * value));
    }

    private void SetPosition(Vector3 position)
    {
        for (var i = 0; i < Elements.Length; i++)
        {
            Elements[i].localPosition = i * position;
        }
    }

    private void RestoreInitialPositions()
    {
        for (var i = 0; i < Elements.Length; i++)
        {
            Elements[i].localPosition = initialPositions[i];
        }
    }
}

public class SmoothStepPositionGroupStateData : BasicMovementStateData
{
    public Vector3 Position;

    public SmoothStepPositionGroupStateData(BaseEvent data) : base(data)
    {
    }
}
