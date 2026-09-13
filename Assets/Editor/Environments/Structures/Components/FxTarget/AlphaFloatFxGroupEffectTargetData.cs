using System.Linq;
using UnityEngine;

public class AlphaFloatFxGroupEffectTargetData : EnvironmentComponentData<AlphaFx>
{
    public int[] MaterialPropertyBlockControllers;
    public string Property;
    public float[] StaticColor;

    public override void FillComponents(GameObject self, AlphaFx comp, CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(container.GetComponentOrNull<MaterialPropertyBlockController>)
            .Where(x => x != null)
            .ToArray();
        comp.Property = Property;
        comp.StaticColor = new Color(StaticColor[0], StaticColor[1], StaticColor[2], StaticColor[3]);
    }
}
