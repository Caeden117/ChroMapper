using UnityEngine;

public class SpectrogramMultiplierFloatFxEffectTargetData : EnvironmentComponentData<SpectrogramMultiplierFx>
{
    public int Spectrogram;

    public override void FillComponents(
        GameObject self,
        SpectrogramMultiplierFx comp,
        CreateContainer container) =>
        comp.SpectrogramRow = container.GetComponentOrNull<SpectrogramRowPropertyAnimator>(Spectrogram);
}
