using UnityEngine;

public class SDFPointData : EnvironmentComponentData<SDFPoint>
{
    public float Radius;

    public override void FillComponents(GameObject self, SDFPoint comp, CreateContainer container)
    {
        comp.Radius = Radius;
        comp.SqrtRadius = Radius;
    }
}
