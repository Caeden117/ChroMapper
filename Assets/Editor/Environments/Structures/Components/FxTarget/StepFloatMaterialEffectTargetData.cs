using UnityEngine;

public class StepFloatMaterialPropertyEffectTargetData : EnvironmentComponentData<MpbStepFx>
{
    public int MaterialPropertyBlockController;
    public string PropertyName;

    public float StepFactor;
    public float StepSize;

    public override void FillComponents(GameObject self, MpbStepFx comp, CreateContainer container)
    {
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.PropertyName = PropertyName;
        comp.StepFactor = StepFactor;
        comp.StepSize = StepSize;
    }
}
