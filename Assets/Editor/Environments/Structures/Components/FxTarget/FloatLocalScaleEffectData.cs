using System.Linq;
using UnityEngine;

public class FloatLocalScaleEffectData : EnvironmentComponentData<LocalScaleFx>
{
    public int[] Transforms;
    public Vector2 ValueBounds;
    public Vector3 StartScale;

    public override void FillComponents(GameObject self, LocalScaleFx comp, CreateContainer container)
    {
        comp.TargetTransforms = Transforms
            .Select(container.GetComponentOrNull<Transform>)
            .Where(x => x != null)
            .Select(x =>
            {
                x.transform.localScale = StartScale;
                return x;
            })
            .ToArray();
        comp.ValueBounds = ValueBounds;
    }
}
