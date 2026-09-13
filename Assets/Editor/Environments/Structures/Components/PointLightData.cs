using UnityEngine;

public class PointLightData : EnvironmentComponentData<PointLight>
{
    public float Intensity;

    public override void FillComponents(GameObject self, PointLight comp, CreateContainer container) =>
        comp.Intensity = Intensity;
}
