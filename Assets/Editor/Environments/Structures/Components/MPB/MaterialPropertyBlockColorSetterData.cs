using UnityEngine;

public class MaterialPropertyBlockColorSetterData : EnvironmentComponentData<MaterialPropertyBlockColorSetter>
{
    public int MaterialPropertyBlockController;
    public string Property;
    public bool InverseAlpha;
    public bool DisableOnZeroAlpha;
    public bool SendAlphaToProperty;
    public string AlphaProperty;
    public bool MultiplyWithAlpha;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockColorSetter comp,
        CreateContainer container)
    {
        comp.Controller =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.Property = Property;
        comp.InverseAlpha = InverseAlpha;
        comp.DisableOnZeroAlpha = DisableOnZeroAlpha;
        comp.SendAlphaToProperty = SendAlphaToProperty;
        comp.AlphaProperty = AlphaProperty;
        comp.MultiplyWithAlpha = MultiplyWithAlpha;
    }
}
