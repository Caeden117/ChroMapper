using UnityEngine;

public class MoveInDirectionEffectData : EnvironmentComponentData<MoveInDirectionFx>
{
    public int Transform;
    public Vector3 MoveOrigin;
    public float MoveScale = 1f;

    public override void FillComponents(GameObject self, MoveInDirectionFx comp, CreateContainer container)
    {
        comp.TargetTransform = container.GetComponentOrNull<Transform>(Transform);
        comp.MoveOrigin = MoveOrigin;
        comp.MoveScale = MoveScale;
    }
}
