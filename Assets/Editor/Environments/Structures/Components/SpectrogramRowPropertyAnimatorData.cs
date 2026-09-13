using UnityEngine;

public class SpectrogramRowPropertyAnimatorData : EnvironmentComponentData<SpectrogramRowPropertyAnimator>
{
    public int MaterialPropertyBlockController;
    public int DataIndex;
    public string PropertyName;
    public float Multiplier;
    public AnimationCurveData AnimationCurve;

    public override void FillComponents(
        GameObject self,
        SpectrogramRowPropertyAnimator comp,
        CreateContainer container)
    {
        comp.SpectrogramDataProvider = container.Descriptor.SpectrogramDataProvider;
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.DataIndex = DataIndex;
        comp.PropertyName = PropertyName;
        comp.Multiplier = Multiplier;
        comp.AnimationCurve = AnimationCurve.Create();
    }
}
