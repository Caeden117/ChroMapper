using UnityEngine;

public class FloatTextureProcessor3DParameterEffectTargetData : EnvironmentComponentData<TextureProcessor3DParameterFx>
{
    public int TextureProcessor3D;

    public int Parameter;
    public int Channel;

    public Vector2 ValueBounds = new(0f, 1f);

    public override void FillComponents(
        GameObject self,
        TextureProcessor3DParameterFx comp,
        CreateContainer container)
    {
        comp.TextureProcessor3D = container.GetComponentOrNull<TextureProcessor3D>(TextureProcessor3D);
        if (comp.TextureProcessor3D == null) comp.TextureProcessor3D = self.GetComponentInParent<TextureProcessor3D>();
        comp.Parameter = (TextureProcessor3DParameterFx.TextureProcessor3DParameter)Parameter;
        comp.Channel = (TextureProcessor3DParameterFx.TextureProcessor3DChannel)Channel;
        comp.ValueBounds = ValueBounds;
    }
}
