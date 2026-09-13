using UnityEngine;

public class DirectionalLightData : EnvironmentComponentData<DirectionalLight>
{
    public float LightIntensity;
    public float LightRadius;

    public override void FillComponents(GameObject self, DirectionalLight comp, CreateContainer container)
    {
        comp.Intensity = LightIntensity;
        comp.Radius = LightRadius;
    }
}
