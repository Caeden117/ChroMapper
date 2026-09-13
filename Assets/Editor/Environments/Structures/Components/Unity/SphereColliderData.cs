using UnityEngine;

public class SphereColliderData : EnvironmentComponentData<SphereCollider>
{
    public Vector3 Center;
    public float Radius;

    public override void FillComponents(GameObject self, SphereCollider comp, CreateContainer container)
    {
        comp.center = Center;
        comp.radius = Radius;
    }
}
