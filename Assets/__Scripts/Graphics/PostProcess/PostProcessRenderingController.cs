using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Built-in pipeline equivalent of the game's MainEffect renderer pass. The
// scene owns this controller and its command buffer.
public sealed class PostProcessRenderingController : MonoBehaviour
{
    [SerializeField] private PyramidBloomController pyramidBloomController;
    [SerializeField] private ChromaticAberrationRenderer chromaticAberrationRenderer;

    private const CameraEvent postProcessCameraEvent = CameraEvent.BeforeImageEffects;

    private static readonly int sourceTextureId = Shader.PropertyToID("_ChroMapperPostProcessSource");
    private static readonly int postBloomOutputId = Shader.PropertyToID("_ChroMapperPostBloomOutput");

    private Camera activeCamera;
    private CommandBuffer commandBuffer;
    private bool active;
    private bool commandBufferAttached;

    public void AssignToCamera(CameraController cameraController)
    {
        DetachCommandBuffer();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        AttachCommandBuffer();
    }

    private void OnEnable() => Activate();

    private void OnDisable() => Deactivate();

    private void OnDestroy()
    {
        Deactivate();
        if (commandBuffer == null) return;
        commandBuffer.Release();
        commandBuffer = null;
    }

    private void Activate()
    {
        if (active) return;
        active = true;
        Camera.onPreRender += OnCameraPreRender;
        AttachCommandBuffer();
    }

    private void Deactivate()
    {
        if (!active) return;
        active = false;
        Camera.onPreRender -= OnCameraPreRender;
        DetachCommandBuffer();
    }

    private void AttachCommandBuffer()
    {
        if (!active || activeCamera == null || commandBufferAttached) return;

        commandBuffer ??= new CommandBuffer { name = "Post Process" };
        activeCamera.AddCommandBuffer(postProcessCameraEvent, commandBuffer);
        commandBufferAttached = true;
    }

    private void DetachCommandBuffer()
    {
        if (!commandBufferAttached) return;
        if (activeCamera != null)
            activeCamera.RemoveCommandBuffer(postProcessCameraEvent, commandBuffer);
        commandBufferAttached = false;
        commandBuffer?.Clear();
    }

    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera || commandBuffer == null) return;

        commandBuffer.Clear();
        var renderPostBloom = pyramidBloomController != null && pyramidBloomController.IsReady;
        var renderChromaticAberration =
            chromaticAberrationRenderer != null && chromaticAberrationRenderer.IsReady;
        var renderFade = pyramidBloomController != null && pyramidBloomController.IsFadeReady;
        if (!renderPostBloom && !renderChromaticAberration && !renderFade) return;

        var sourceDescriptor = GetCopyDescriptor(GetCameraTargetDescriptor());
        var sourceWidth = sourceDescriptor.width;
        var sourceHeight = sourceDescriptor.height;
        var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

        if (!renderPostBloom && !renderChromaticAberration)
        {
            pyramidBloomController.RecordFade(commandBuffer, cameraTarget);
            return;
        }

        commandBuffer.GetTemporaryRT(sourceTextureId, sourceDescriptor, FilterMode.Bilinear);
        commandBuffer.Blit(cameraTarget, sourceTextureId);

        if (renderPostBloom && renderChromaticAberration)
        {
            commandBuffer.GetTemporaryRT(
                postBloomOutputId, sourceDescriptor, FilterMode.Bilinear);
            pyramidBloomController.RecordRender(
                commandBuffer, sourceTextureId, sourceWidth, sourceHeight, postBloomOutputId);
            chromaticAberrationRenderer.RecordRender(
                commandBuffer, postBloomOutputId, cameraTarget, sourceWidth, sourceHeight);
            commandBuffer.ReleaseTemporaryRT(postBloomOutputId);
        }
        else if (renderPostBloom)
        {
            pyramidBloomController.RecordRender(
                commandBuffer, sourceTextureId, sourceWidth, sourceHeight, cameraTarget);
        }
        else if (renderChromaticAberration)
        {
            chromaticAberrationRenderer.RecordRender(
                commandBuffer, sourceTextureId, cameraTarget, sourceWidth, sourceHeight);
            if (renderFade) pyramidBloomController.RecordFade(commandBuffer, cameraTarget);
        }
        else
        {
            pyramidBloomController.RecordFade(commandBuffer, cameraTarget);
        }

        commandBuffer.ReleaseTemporaryRT(sourceTextureId);
    }

    private RenderTextureDescriptor GetCameraTargetDescriptor()
    {
        if (activeCamera.targetTexture != null)
        {
            var descriptor = activeCamera.targetTexture.descriptor;
            descriptor.width = Mathf.Max(activeCamera.scaledPixelWidth, 1);
            descriptor.height = Mathf.Max(activeCamera.scaledPixelHeight, 1);
            return descriptor;
        }

        return new RenderTextureDescriptor(
            Mathf.Max(activeCamera.scaledPixelWidth, 1),
            Mathf.Max(activeCamera.scaledPixelHeight, 1),
            activeCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default,
            0)
        {
            sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
        };
    }

    private static RenderTextureDescriptor GetCopyDescriptor(RenderTextureDescriptor descriptor)
    {
        descriptor.depthBufferBits = 0;
        descriptor.depthStencilFormat = GraphicsFormat.None;
        descriptor.msaaSamples = 1;
        descriptor.useDynamicScale = false;
        descriptor.useDynamicScaleExplicit = false;
        // The game composites over an LDR scene buffer, so alpha clamps at
        // write time. Light-driven alphas intentionally exceed 1 on glow
        // materials; a UNORM copy preserves the downstream alpha-gate and
        // boost math the game's binaries encode. 16-bit UNORM keeps that
        // clamp without adding 8-bit banding on top of the game's look.
        descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
        return descriptor;
    }
}
