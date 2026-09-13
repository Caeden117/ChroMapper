using UnityEngine;

public class CapsuleColliderData : EnvironmentComponentData<CapsuleCollider>
{
    public Vector3 Center;
    public float Radius;
    public float Height;
    public int Direction;

    public override void FillComponents(GameObject self, CapsuleCollider comp, CreateContainer container)
    {
        comp.center = Center;
        comp.radius = Radius;
        comp.height = Height;
        comp.direction = Direction;
    }
}
