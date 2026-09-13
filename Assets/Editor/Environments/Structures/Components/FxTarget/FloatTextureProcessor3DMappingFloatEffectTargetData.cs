using UnityEngine;

public class
    FloatTextureProcessor3DMappingFloatEffectTargetData : EnvironmentComponentData<TextureProcessor3DMappingFloatFx>
{
    public string Material;
    public bool UseSlave;
    public string SlaveMaterial;

    public int Mapping;

    public Vector2 ValueBounds = new(-1f, 1f);
    public bool InvertAxis;
    public bool InvertAxisSlave;

    public override void FillComponents(
        GameObject self,
        TextureProcessor3DMappingFloatFx comp,
        CreateContainer container)
    {
        comp.Material = container.GetMaterialSafe(Material);
        comp.SlaveMaterial = container.GetMaterialSafe(SlaveMaterial);
        comp.UseSlave = UseSlave;
        comp.Mapping = (TextureProcessor3DMappingFloatFx.TextureProcessor3DMapping)Mapping;
        comp.ValueBounds = ValueBounds;
        comp.InvertAxis = InvertAxis;
        comp.InvertAxisSlave = InvertAxisSlave;
    }
}
