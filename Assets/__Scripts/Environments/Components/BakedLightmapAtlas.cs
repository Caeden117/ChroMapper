using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public sealed class BakedLightmapAtlas : MonoBehaviour
{
    public int Index;
    public Vector4 ScaleOffset = new(1f, 1f, 0f, 0f);

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        Apply();
    }

    public void Configure(MeshRenderer renderer, int index, Vector4 scaleOffset)
    {
        meshRenderer = renderer;
        Index = index;
        ScaleOffset = scaleOffset;
        Apply();
    }

    private void Apply()
    {
        // Unity does not save these renderer properties without a LightingDataAsset. Keep the native atlas rectangle for scene loads and Chroma clones.
        meshRenderer.lightmapIndex = Index;
        meshRenderer.lightmapScaleOffset = ScaleOffset;
    }
}
