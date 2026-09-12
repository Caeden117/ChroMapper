using UnityEngine;

public class TrackLaneRingsRotationEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsRotationEffect>
{
    public EnvironmentEventType EventType;
    public int TrackLaneRingsRotationEffect;
    public float Rotation;
    public float RotationStep;
    public EnvironmentRotationStepType RotationStepType;
    public int RotationPropagationSpeed;
    public float RotationFlexySpeed;

    public override void FillComponents(
        GameObject self,
        TrackLaneRingsRotationEffect comp,
        CreateContainer container)
    {
        container.Descriptor.BasicEventEffectManager.Register(EventType, comp);

        comp.Visual = container
            .GetComponentOrNull<TrackLaneRingsRotation>(TrackLaneRingsRotationEffect);
        comp.Rotation = Rotation;
        comp.Step = RotationStep;
        comp.StepType = RotationStepType;
        comp.PropagationSpeed = RotationPropagationSpeed;
        comp.FlexySpeed = RotationFlexySpeed;
    }
}
