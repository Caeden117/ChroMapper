using UnityEngine;

public class SmoothStepPositionGroupEventEffectData : EnvironmentComponentData<SmoothStepPositionGroupEventEffect>
{
    public int GroupMinY;
    public int GroupMaxY;
    public float GroupStepSize;
    public Vector3 GroupStartPos;
    public string GroupEasing;

    public override void FillComponents(
        GameObject self,
        SmoothStepPositionGroupEventEffect comp,
        CreateContainer container)
    {
        comp.ClampValue = true;
        comp.MinValue = GroupMinY;
        comp.MaxValue = GroupMaxY;
        comp.BaseOffset = Vector3.zero;
        comp.MovementVector = Vector3.forward;
        comp.StepSize = GroupStepSize;

        container.Descriptor.BasicEventEffectManager.Register(9, comp);
    }
}
