using UnityEngine;

public class TrackLaneRingsRotationEffectData : EnvironmentComponentData<TrackLaneRingsRotation>
{
    public int TrackLaneRingsManager;
    public float StartupRotationAngle;
    public float StartupRotationStep;
    public int StartupRotationPropagationSpeed;
    public float StartupRotationFlexySpeed;

    public override void FillComponents(
        GameObject self,
        TrackLaneRingsRotation comp,
        CreateContainer container)
    {
        comp.Manager = container.GetComponentOrNull<TrackLaneRingsManager>(TrackLaneRingsManager);
        comp.StartupRotationAngle = StartupRotationAngle;
        comp.StartupRotationStep = StartupRotationStep;
        comp.StartupRotationPropagationSpeed = StartupRotationPropagationSpeed;
        comp.StartupRotationFlexySpeed = StartupRotationFlexySpeed;

        foreach (var r in comp.Manager.Rings) r.transform.localEulerAngles = new Vector3(0, 0, StartupRotationAngle);
    }
}
