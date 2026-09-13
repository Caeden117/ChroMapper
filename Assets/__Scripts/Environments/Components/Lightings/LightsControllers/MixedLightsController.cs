using UnityEngine;

public class MixedLightsController : CombinedLightsController
{
    public MaterialPropertyBlockColorSetter MpbColorSetter;

    public float LightMultiplier = 1f;

    protected override bool Initialize() => MpbColorSetter != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;
        MpbColorSetter.SetColor(color * LightMultiplier);
    }
}
