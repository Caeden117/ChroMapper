using UnityEngine;

public class SmoothStepPositionEventEffectData : EnvironmentComponentData<SmoothStepPositionEventEffect>
{
    public EnvironmentEventType EventType;
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;

    public override void FillComponents(
        GameObject self,
        SmoothStepPositionEventEffect comp,
        CreateContainer container)
    {
        container.Descriptor.BasicEventEffectManager.Register(EventType, comp);

        comp.ClampValue = ClampValue;
        comp.MinY = MinY;
        comp.MaxY = MaxY;
        comp.MovementVector = MovementVector;
        comp.StepSize = StepSize;
    }
}
