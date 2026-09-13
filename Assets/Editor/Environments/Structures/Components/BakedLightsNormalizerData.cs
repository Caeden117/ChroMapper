using UnityEngine;

public class BakedLightsNormalizerData : EnvironmentComponentData<BakedLightsNormalizer>
{
    public float MaxTotalIntensity = 1f;

    public override void FillComponents(GameObject self, BakedLightsNormalizer comp, CreateContainer container) =>
        comp.MaxTotalIntensity = MaxTotalIntensity;
}
