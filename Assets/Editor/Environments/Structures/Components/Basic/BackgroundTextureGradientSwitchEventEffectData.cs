using UnityEngine;

public class BackgroundTextureGradientSwitchEventEffectData : EnvironmentComponentData<BackgroundTextureGradientSwitch>
{
    public int DefaultTextureGradient;
    public int BoostTextureGradient;

    public override void FillComponents(
        GameObject self,
        BackgroundTextureGradientSwitch comp,
        CreateContainer container)
    {
        comp.Effect = container.Descriptor.BasicEventEffectManager.GetEffect<ColorBoostEffect>(5);

        comp.DefaultTextureGradient = container
            .GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(DefaultTextureGradient);
        comp.BoostTextureGradient = container
            .GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(BoostTextureGradient);
    }
}
