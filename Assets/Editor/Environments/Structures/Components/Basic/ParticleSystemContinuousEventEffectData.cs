using System.Linq;
using UnityEngine;

public class ParticleSystemContinuousEventEffectData : EnvironmentComponentData<ParticleSystemContinuous>
{
    public EnvironmentEventType EventType;
    public int[] ParticleSystems;

    public override void FillComponents(
        GameObject self,
        ParticleSystemContinuous comp,
        CreateContainer container)
    {
        comp.Effect = container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
            EventType);

        comp.ParticleSystems = ParticleSystems
            .Select(container.GetComponentOrNull<ParticleSystem>)
            .Where(y => y != null)
            .ToArray();
    }
}
