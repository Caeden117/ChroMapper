using UnityEngine;

public class HydraulicCarJumpEffectData : EnvironmentComponentData<HydraulicCarJump>
{
    public EnvironmentEventType EventType;
    public int[] EventValues;
    public Vector3 Impulse;
    public float Randomness = 0.1f;
    public Vector3 Position;
    public float MinDelayBetweenEvents = 0.5f;
    public int Rigidbody;

    public override void FillComponents(GameObject self, HydraulicCarJump comp, CreateContainer container)
    {
        comp.Effect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                EventType);

        comp.Rigidbody = container.GetComponentOrNull<Rigidbody>(Rigidbody);
        comp.EventValues = EventValues;
        comp.Impulse = Impulse;
        comp.Randomness = Randomness;
        comp.Position = Position;
        comp.MinDelayBetweenEvents = MinDelayBetweenEvents;
    }
}
