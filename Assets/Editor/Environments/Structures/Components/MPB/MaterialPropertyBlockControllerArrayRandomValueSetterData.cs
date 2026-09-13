using System.Linq;
using UnityEngine;

public class
    MaterialPropertyBlockControllerArrayRandomValueSetterData : EnvironmentComponentData<
    MaterialPropertyBlockControllerArrayRandomValueSetter>
{
    public int[] MaterialPropertyBlockControllers;
    public string PropertyName;
    public Vector3 Min;
    public Vector3 Max;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockControllerArrayRandomValueSetter comp,
        CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(container.GetComponentOrNull<MaterialPropertyBlockController>)
            .ToArray();
        comp.PropertyName = PropertyName;
        comp.Min = Min;
        comp.Max = Max;
    }
}
