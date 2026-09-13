using UnityEngine;

public class ColliderEventEffectData : EnvironmentComponentData<ColliderFx>
{
    public int EffectCollider;
    public float Value;

    public override void FillComponents(GameObject self, ColliderFx comp, CreateContainer container)
    {
        comp.Repository = container.Descriptor.FloatFxGroupEffectManager.gameObject
            .GetOrAddComponent<ColliderRepository>();

        var coll = container.GetComponentOrNull<Collider>(EffectCollider);
        comp.Collider = coll;
        comp.Value = Value;
    }
}
