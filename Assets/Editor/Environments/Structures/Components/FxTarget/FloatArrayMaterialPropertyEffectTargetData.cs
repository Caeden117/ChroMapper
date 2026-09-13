using System.Linq;
using UnityEngine;

public class FloatArrayMaterialPropertyEffectTargetData : EnvironmentComponentData<MpbArrayFx>
{
    public int[] MaterialPropertyBlockControllers;
    public string PropertyName;

    public Vector2 ValueBounds;
    public float GranularityMultiplier;

    public override void FillComponents(GameObject self, MpbArrayFx comp, CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(container.GetComponentOrNull<MaterialPropertyBlockController>)
            .Where(x => x != null)
            .ToArray();
        comp.PropertyName = PropertyName;
        comp.ValueBounds = ValueBounds;
        comp.GranularityMultiplier = GranularityMultiplier;
    }
}
