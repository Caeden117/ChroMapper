using System.Linq;
using UnityEngine;

public class FloatFxGroupEffectCollectionTargetData : EnvironmentComponentData<CollectionFx>
{
    public int[] FloatFxGroupEffectTargets;

    public override void FillComponents(GameObject self, CollectionFx comp, CreateContainer container)
    {
        comp.Targets = FloatFxGroupEffectTargets
            .Select(container.GetComponentOrNull<FxTarget>)
            .Where(x => x != null)
            .ToArray();
    }
}
