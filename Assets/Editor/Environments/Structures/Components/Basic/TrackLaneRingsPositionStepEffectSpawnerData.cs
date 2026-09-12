using UnityEngine;

public class TrackLaneRingsPositionStepEffectSpawnerData : EnvironmentComponentData<TrackLaneRingsPositionSpawner>
{
    public EnvironmentEventType EventType;
    public int TrackLaneRingsManager;
    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    public override void FillComponents(
        GameObject self,
        TrackLaneRingsPositionSpawner comp,
        CreateContainer container)
    {
        comp.RingManager = container
            .GetComponentOrNull<TrackLaneRingsManager>(TrackLaneRingsManager);
        comp.MinPositionStep = MinPositionStep;
        comp.MaxPositionStep = MaxPositionStep;
        comp.MoveSpeed = MoveSpeed;

        var effect = self.AddComponent<TrackLaneRingsPositionEffect>();
        effect.Visual = comp;
        comp.EffectManager = effect;
        container.Descriptor.BasicEventEffectManager.Register(EventType, effect);
    }
}
