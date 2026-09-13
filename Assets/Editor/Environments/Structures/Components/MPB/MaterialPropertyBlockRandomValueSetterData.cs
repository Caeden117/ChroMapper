using System.Linq;
using UnityEngine;

public class
    MaterialPropertyBlockRandomValueSetterData : EnvironmentComponentData<MaterialPropertyBlockRandomValueSetter>
{
    public int[] Renderers;
    public string PropertyName;
    public float MinValue;
    public float MaxValue = 1f;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockRandomValueSetter comp,
        CreateContainer container)
    {
        comp.Renderers = Renderers.Select(container.GetComponentOrNull<Renderer>).ToArray();
        comp.PropertyName = PropertyName;
        comp.MinValue = MinValue;
        comp.MaxValue = MaxValue;
    }
}
