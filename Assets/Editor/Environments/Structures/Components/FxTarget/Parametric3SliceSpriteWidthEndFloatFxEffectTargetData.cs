using UnityEngine;

public class Parametric3SliceSpriteWidthEndFloatFxEffectTargetData : EnvironmentComponentData<ParametricSliceEndWidthFx>
{
    public int Parametric3SliceSpriteController;

    public Vector2 ValueBounds;
    public float ValueMultiplier = 1f;

    public override void FillComponents(
        GameObject self,
        ParametricSliceEndWidthFx comp,
        CreateContainer container)
    {
        comp.SpriteLight = container.GetComponentOrNull<ParametricSpriteLight>(Parametric3SliceSpriteController);
        comp.ValueBounds = ValueBounds;
        comp.ValueMultiplier = ValueMultiplier;
    }
}
