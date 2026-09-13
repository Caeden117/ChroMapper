using System.Linq;
using UnityEngine;

public class
    FloatTextureProcessor3DMaterialSwitchEffectTargetData : EnvironmentComponentData<TextureProcessor3DMaterialSwitchFx>
{
    public string[] MaterialArray;

    public Vector2 ValueBounds = new(-10f, 10f);

    public int[] GridElementControllers;
    public int MaterialIndex;

    public override void FillComponents(
        GameObject self,
        TextureProcessor3DMaterialSwitchFx comp,
        CreateContainer container)
    {
        comp.MaterialArray = MaterialArray
            .Select(container.GetMaterialSafe)
            .ToArray();
        comp.GridElementControllers =
            GridElementControllers.Select(container.GetComponentOrNull<GridElementController>).ToArray();
        comp.ValueBounds = ValueBounds;
        comp.MaterialIndex = MaterialIndex;
    }
}
