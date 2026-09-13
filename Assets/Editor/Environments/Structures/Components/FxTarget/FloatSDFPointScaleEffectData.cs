using UnityEngine;

public class FloatSDFPointScaleEffectData : EnvironmentComponentData<SDFPointScaleFx>
{
    public int ColorPoint;
    public Vector2 ValueBounds;

    public override void FillComponents(GameObject self, SDFPointScaleFx comp, CreateContainer container)
    {
        comp.ColorPoint = container.GetComponentOrNull<SDFPoint>(ColorPoint);
        if (comp.ColorPoint == null) comp.ColorPoint = comp.GetComponent<SDFPoint>();
        comp.ValueBounds = ValueBounds;
    }
}
