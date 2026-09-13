using UnityEngine;

public class BakedReflectionProbeData : EnvironmentComponentData<BakedReflectionProbe>
{
    public Vector3 Size;
    public Vector3 Offset;
    public int ResolutionBeforeDownsample;
    public int DownsampleByHalfCount;

    public override void FillComponents(
        GameObject self,
        BakedReflectionProbe comp,
        CreateContainer container)
    {
        comp.Size = Size;
        comp.Offset = Offset;
        comp.ResolutionBeforeDownsample = ResolutionBeforeDownsample;
        comp.DownsampleByHalfCount = DownsampleByHalfCount;
    }
}
