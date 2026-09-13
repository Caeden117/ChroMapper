using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class TransformData : EnvironmentComponentData<Transform>
{
    public override bool AllowNew => false;

    public Vector3 Position;
    public Vector3 LocalPosition;

    public Vector3 Rotation;
    public Vector3 LocalRotation;

    public Vector3 Scale = Vector3.one;

    public override void FillComponents(GameObject self, Transform comp, CreateContainer container)
    {
        comp.localPosition = LocalPosition;
        comp.localEulerAngles = LocalRotation;
        comp.localScale = Scale;
    }
}
