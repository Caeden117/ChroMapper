using UnityEngine;
using UnityEditor;

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
        // Environment exports describe probe bounds but omit the packed cubemaps. Restore the imported native pair when regenerating a scene.
        comp.ReflectionProbeData = AssetDatabase.LoadAssetAtPath<ReflectionProbeDataSO>(PathUtils.Combine(
            "Assets", "_Graphics", "Textures", "Environments", "BakedLighting",
            container.EnvironmentId + "ReflectionProbeData.asset"));
        comp.Size = Size;
        comp.Offset = Offset;
        comp.ResolutionBeforeDownsample = ResolutionBeforeDownsample;
        comp.DownsampleByHalfCount = DownsampleByHalfCount;
    }
}
