using Beatmap.Base;
using UnityEngine;

public class SmoothStepPositionEventEffect : BasicMovementEffect<SmoothStepPositionStateData>
{
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;

    private Transform tr;
    private Vector3 initPos;
    private readonly Vector3Tween tween = new();

    private void Awake()
    {
        tr = transform;
        initPos = tr.localPosition;
        tween.Easing = Easing.Cubic.InOut;
    }

    protected override SmoothStepPositionStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(SmoothStepPositionStateData previous, SmoothStepPositionStateData current)
    {
        current.StartPosition = previous == null
            ? initPos
            : GetPositionForValue(current.Base.Value);
    }

    protected override void ApplyVisual(float beat, float seconds, SmoothStepPositionStateData current, SmoothStepPositionStateData next)
    {
        if (next == null)
        {
            tr.localPosition = current.StartPosition;
            return;
        }

        tween.StartValue = current.StartPosition;
        tween.EndValue = next.StartPosition;
        tween.StartTime = current.StartTime;
        tween.EndTime = next.StartTime;

        tween.UpdateTime(beat);
        tr.localPosition = tween.Current;
    }

    private Vector3 GetPositionForValue(int value)
    {
        if (ClampValue)
            value = Mathf.Clamp(value, MinY, MaxY);

        return initPos + (MovementVector * (StepSize * (value - 4)));
    }
}

public class SmoothStepPositionStateData : BasicMovementStateData
{
    public Vector3 StartPosition;

    public SmoothStepPositionStateData(BaseEvent data) : base(data)
    {
    }
}
