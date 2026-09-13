using UnityEngine;

public class FloatMaterialPropertyEffectTargetData : EnvironmentComponentData<MpbFx>
{
    public int MaterialPropertyBlockController;
    public string PropertyName;
    public Vector2 ValueBounds;
    public float GranularityMultiplier;

    public override void FillComponents(GameObject self, MpbFx comp, CreateContainer container)
    {
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.PropertyName = PropertyName;
        comp.ValueBounds = ValueBounds;
        comp.GranularityMultiplier = GranularityMultiplier;
    }
}
