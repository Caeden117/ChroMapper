using UnityEngine;

public class
    BloomPrePassBackgroundNonLightRendererData : EnvironmentComponentData<BloomPrePassBackgroundNonLightRenderer>
{
    public int ExecutionTimeType;
    public bool KeepDefaultRendering;
    public bool UseCustomMaterial;
    public string CustomMaterial;
    public bool UseCustomPropertyBlock;
    public int Renderer;
    public int MeshFilter;

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundNonLightRenderer comp,
        CreateContainer container)
    {
        comp.CustomMaterial = container.GetMaterialSafe(CustomMaterial);
        comp.Renderer = container.GetComponentOrNull<Renderer>(Renderer);
        comp.MeshFilter = container.GetComponentOrNull<MeshFilter>(MeshFilter);
        comp.ExecutionTimeType = (BloomPrePassNonLightPass.ExecutionTime)ExecutionTimeType;
        comp.KeepDefaultRendering = KeepDefaultRendering;
        comp.UseCustomMaterial = UseCustomMaterial;
        comp.UseCustomPropertyBlock = UseCustomPropertyBlock;
    }
}
