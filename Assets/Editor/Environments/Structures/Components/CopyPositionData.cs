using UnityEngine;
using UnityEngine.Animations;

public class CopyPositionData : EnvironmentComponentData<PositionConstraint>
{
    public int Transform;

    public override void FillComponents(GameObject self, PositionConstraint comp, CreateContainer container)
    {
        var t = container.GetComponentOrNull<Transform>(Transform);
        t.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.AddSource(new ConstraintSource { sourceTransform = t.transform, weight = 1 });
        comp.constraintActive = true;
    }
}
