using UnityEngine;

public class VertexDisplacementFloatFxGroupEffectTargetData : EnvironmentComponentData<VertexDisplacementFx>
{
    public Vector3 DisplacementRanges;
    public AnimationCurveData XAnimationCurve;
    public AnimationCurveData YAnimationCurve;
    public AnimationCurveData ZAnimationCurve;
    public int DisplacementController;
    public int Renderer;
    public bool UseTestValue;
    public float TestFloatValue;

    public override void FillComponents(GameObject self, VertexDisplacementFx comp, CreateContainer container)
    {
        comp.DisplacementController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(DisplacementController);
        comp.Renderer = container.GetComponentOrNull<Renderer>(Renderer);
        comp.DisplacementRanges = DisplacementRanges;
        comp.XAnimationCurve = XAnimationCurve.Create();
        comp.YAnimationCurve = YAnimationCurve.Create();
        comp.ZAnimationCurve = ZAnimationCurve.Create();
        comp.UseTestValue = UseTestValue;
        comp.TestFloatValue = TestFloatValue;
    }
}
