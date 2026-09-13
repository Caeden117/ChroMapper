using UnityEngine;

public class
    MaterialPropertyBlockControllerRandomValueSetterData : EnvironmentComponentData<
    MaterialPropertyBlockControllerRandomValueSetter>
{
    public int MaterialPropertyBlockController;
    public string PropertyName;
    public float Min;
    public float Max = 1000f;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockControllerRandomValueSetter comp,
        CreateContainer container)
    {
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.PropertyName = PropertyName;
        comp.Min = Min;
        comp.Max = Max;
    }
}
