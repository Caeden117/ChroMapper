using UnityEngine;

public class ParticleSystemRendererData : EnvironmentComponentData<ParticleSystemRenderer>
{
    public override void FillComponents(GameObject self, ParticleSystemRenderer comp, CreateContainer container)
    {
        var visibility = self.GetOrAddComponent<WorldParticleVisibility>();
        visibility.enabled = true;
        visibility.Renderer = comp;
        visibility.IsSmoke = WorldParticleVisibility.IsSmokeMaterial(comp);
    }
}
