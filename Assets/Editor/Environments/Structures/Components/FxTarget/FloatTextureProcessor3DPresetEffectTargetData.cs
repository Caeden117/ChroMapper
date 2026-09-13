using UnityEngine;

public class FloatTextureProcessor3DPresetEffectTargetData : EnvironmentComponentData<TextureProcessor3DPresetFx>
{
    public int TextureProcessor3D;
    public Vector2 ValueBounds = new(0f, 1f);

    public override void FillComponents(
        GameObject self,
        TextureProcessor3DPresetFx comp,
        CreateContainer container)
    {
        comp.TextureProcessor3D = container.GetComponentOrNull<TextureProcessor3D>(TextureProcessor3D);
        if (comp.TextureProcessor3D == null) comp.TextureProcessor3D = self.GetComponentInParent<TextureProcessor3D>();
        comp.ValueBounds = ValueBounds;
    }
}
