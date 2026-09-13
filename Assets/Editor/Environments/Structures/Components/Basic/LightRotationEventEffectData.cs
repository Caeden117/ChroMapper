using UnityEngine;

public class LightRotationEventEffectData : EnvironmentComponentData<LightRotation>
{
    public EnvironmentEventType EventType;
    public Vector3 RotationVector;
    public float RotationSpeedMultiplier;

    public override void FillComponents(GameObject self, LightRotation comp, CreateContainer container)
    {
        comp.enabled = true;
        comp.Transform = self.transform;
        comp.StartRotation = self.transform.rotation;
        comp.RotationVector = RotationVector;
        comp.SpeedMultiplier = RotationSpeedMultiplier;

        var effect = self.AddComponent<LightRotationEffect>();
        effect.Visual = comp;
        effect.SpeedMultiplier = RotationSpeedMultiplier;
        container.Descriptor.BasicEventEffectManager.Register(EventType, effect);
    }
}
