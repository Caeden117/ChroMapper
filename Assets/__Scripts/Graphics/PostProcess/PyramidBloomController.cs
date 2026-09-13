using UnityEngine;
using UnityEngine.Rendering;

// Scene-owned equivalent of the game's PyramidBloomSO. It owns the
// bloom parameters, the post-bloom compositor, and the no-post-process fade.
public sealed class PyramidBloomController : MonoBehaviour
{
    [SerializeField] private BloomRenderer bloomRenderer;
    [SerializeField] private Shader fadeShader;
    [SerializeField] private Shader postBloomShader;

    [SerializeField, Range(0f, 5f)] private float bloomBlendFactor = 0.3f;
    [SerializeField] private int bloomTextureWidth = 928;
    [SerializeField, Range(0f, 1f)] private float fade = 1f;
    [SerializeField, Range(0f, 3f)] private float baseColorBoost = 1f;
    [SerializeField] private float baseColorBoostThreshold;

    private static readonly int postBloomTextureId = Shader.PropertyToID("_PostBloomTexture");
    private static readonly int sourceTexelSizeId = Shader.PropertyToID("_PostBloomSourceTexelSize");
    private static readonly int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    private static readonly int fadeId = Shader.PropertyToID("_Fade");
    private static readonly int baseColorBoostId = Shader.PropertyToID("_BaseColorBoost");
    private static readonly int baseColorBoostThresholdId = Shader.PropertyToID("_BaseColorBoostThreshold");
    private static readonly int bloomTextureId = Shader.PropertyToID("_ChroMapperPostBloomTexture");

    private Material fadeMaterial;
    private Material postBloomMaterial;

    public bool IsReady =>
        isActiveAndEnabled && bloomRenderer != null && bloomRenderer.IsReady && postBloomMaterial != null;

    public bool IsFadeReady =>
        isActiveAndEnabled && fadeMaterial != null && fade < 1f;

    private void Awake()
    {
        fadeMaterial = new Material(fadeShader) { hideFlags = HideFlags.HideAndDontSave };
        postBloomMaterial = new Material(postBloomShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    private void OnDestroy()
    {
        if (fadeMaterial != null) Destroy(fadeMaterial);
        if (postBloomMaterial != null) Destroy(postBloomMaterial);
    }

    public void ApplyPreRenderState()
    {
        Shader.SetGlobalFloat(baseColorBoostId, baseColorBoost);
        Shader.SetGlobalFloat(baseColorBoostThresholdId, baseColorBoostThreshold);
    }

    public void RecordRender(
        CommandBuffer commandBuffer,
        RenderTargetIdentifier source,
        int sourceWidth,
        int sourceHeight,
        RenderTargetIdentifier destination)
    {
        if (!IsReady) return;

        var bloomDescriptor = bloomRenderer.GetOutputDescriptor(
            sourceWidth, sourceHeight, bloomTextureWidth);
        commandBuffer.GetTemporaryRT(bloomTextureId, bloomDescriptor, FilterMode.Bilinear);
        bloomRenderer.RecordRender(
            commandBuffer, source, sourceWidth, sourceHeight, bloomTextureWidth, bloomTextureId);

        postBloomMaterial.SetFloat(bloomIntensityId, bloomBlendFactor);
        postBloomMaterial.SetFloat(fadeId, fade);
        commandBuffer.SetGlobalTexture(postBloomTextureId, bloomTextureId);
        commandBuffer.SetGlobalVector(
            sourceTexelSizeId, GetTexelSize(sourceWidth, sourceHeight));
        commandBuffer.Blit(source, destination, postBloomMaterial);
        commandBuffer.SetGlobalTexture(postBloomTextureId, Texture2D.blackTexture);
        commandBuffer.ReleaseTemporaryRT(bloomTextureId);
    }

    public void RecordFade(CommandBuffer commandBuffer, RenderTargetIdentifier destination)
    {
        if (fadeMaterial == null || fade >= 1f) return;
        fadeMaterial.color = new Color(0f, 0f, 0f, 1f - fade);
        commandBuffer.SetRenderTarget(destination);
        commandBuffer.DrawProcedural(
            Matrix4x4.identity, fadeMaterial, 0, MeshTopology.Triangles, 3, 1);
    }

    private static Vector4 GetTexelSize(int width, int height) =>
        new(1f / width, 1f / height, width, height);
}
