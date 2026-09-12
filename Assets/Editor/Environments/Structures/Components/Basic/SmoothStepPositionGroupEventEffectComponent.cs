using UnityEngine;

// TheSecondRingZoom* maps the renamed 1.44.1 export fields into ChroMapper's deterministic group-spacing effect.
public class SmoothStepPositionGroupEventEffectComponent
{
    public int GroupMinY;
    public int GroupMaxY;
    public float GroupStepSize;
    public Vector3 GroupStartPos;
    public string GroupEasing;
    public bool IsEnabled;

    public void CopyTo(SmoothStepPositionGroupEventEffect target)
    {
        target.ClampValue = true;
        target.MinValue = GroupMinY;
        target.MaxValue = GroupMaxY;
        // UGEcko's EnvironmentData group-effect record omits Beat Saber's private _baseOffset, so generated data remains zero
        // and the The Second-only runtime effect replaces its Z component with the known serialized value.
        target.BaseOffset = Vector3.zero;
        target.MovementVector = Vector3.forward;
        target.StepSize = GroupStepSize;
    }
}
