using UnityEngine;

public class SpectrogramMultiplierFx : FxTarget
{
    [SerializeField] public SpectrogramRowPropertyAnimator SpectrogramRow;

    public override void SetValue(int group, int id, float value) => SpectrogramRow.SetMultiplier(value);
    public override void TriggerValue(int group, int id, float value) => SpectrogramRow.SetMultiplier(value);
}
