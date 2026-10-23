using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public sealed class PostProcessRenderingController : MonoBehaviour
{
    [SerializeField] private PyramidBloomController pyramidBloomController;
    [SerializeField] private ChromaticAberrationRenderer chromaticAberrationRenderer;

    private const CameraEvent postProcessCameraEvent = CameraEvent.BeforeImageEffects;

    private static readonly int sourceTextureId = Shader.PropertyToID("_ChroMapperPostProcessSource");
    private static readonly int mainEffectSourceId = Shader.PropertyToID("_ChroMapperMainEffectSource");
    private static readonly int postBloomOutputId = Shader.PropertyToID("_ChroMapperPostBloomOutput");

    private Camera activeCamera;
    private CommandBuffer commandBuffer;
    private bool active;
    private bool commandBufferAttached;
    private bool vivifyBlitsEnabled;
    private VivifyRenderingManager vivify;

    public void AttachVivify(VivifyRenderingManager rendering)
    {
        if (vivify != null) vivify.AssignToCamera(null);
        vivify = rendering;
        if (vivify != null)
        {
            vivify.Cameras.RenderMainEffect = RenderCaptureMainEffect;
            vivify.AssignToCamera(activeCamera, vivifyBlitsEnabled);
        }
        var cameraSetup = FindFirstObjectByType<PerCameraShaderSetupController>();
        if (cameraSetup != null) cameraSetup.VivifyCameras = vivify?.Cameras;
        var bloomFog = FindFirstObjectByType<BloomfogRenderingController>();
        if (bloomFog != null) bloomFog.VivifyCameras = vivify?.Cameras;
        var displacement = FindFirstObjectByType<ScreenDisplacementRenderingController>();
        if (displacement != null) displacement.VivifyCameras = vivify?.Cameras;
        if (vivify != null)
            vivify.Cameras.AfterCaptureRender = camera =>
            {
                if (bloomFog != null) bloomFog.RestoreCameraState(camera);
                if (displacement != null) displacement.ReleaseCaptureBuffer(camera);
            };
    }

    /// <summary>Moves post-processing and camera captures to the selected camera, enabling map blits only for the player camera.</summary>
    /// <param name="cameraController">Selected camera, or null to detach rendering.</param>
    /// <param name="enableVivifyBlits">Whether the selected camera is the player camera.</param>
    public void AssignToCamera(CameraController cameraController, bool enableVivifyBlits = false)
    {
        DetachCommandBuffer();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        vivifyBlitsEnabled = enableVivifyBlits;
        AttachCommandBuffer();
        vivify?.AssignToCamera(activeCamera, vivifyBlitsEnabled);
    }

    private void OnEnable() => Activate();

    private void OnDisable() => Deactivate();

    private void OnDestroy()
    {
        AttachVivify(null);
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
        var before = vivifyBlitsEnabled && vivify?.HasBlits(VivifyRenderOrder.BeforeMainEffect) == true;
        var after = vivifyBlitsEnabled && vivify?.HasBlits(VivifyRenderOrder.AfterMainEffect) == true;
        var mainEffect = vivify == null || vivify.Cameras.MainEffectEnabled(renderingCamera);
        if (!before && !after && (!mainEffect || !HasMainEffect)) return;
        var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        if (!before && !after && pyramidBloomController != null && pyramidBloomController.IsFadeReady
            && !pyramidBloomController.IsReady
            && (chromaticAberrationRenderer == null || !chromaticAberrationRenderer.IsReady))
        {
            pyramidBloomController.RecordFade(commandBuffer, cameraTarget);
            return;
        }

        var descriptor = before || after ? VivifyRenderingManager.GetDescriptor(activeCamera)
            : GetCopyDescriptor(GetCameraTargetDescriptor());
        commandBuffer.GetTemporaryRT(sourceTextureId, descriptor, FilterMode.Bilinear);
        if (before)
            vivify.RecordBlits(commandBuffer, VivifyRenderOrder.BeforeMainEffect, cameraTarget, sourceTextureId, descriptor);
        else commandBuffer.Blit(cameraTarget, sourceTextureId);

        if (mainEffect) RecordMainEffect(commandBuffer, sourceTextureId, cameraTarget, descriptor);
        else commandBuffer.Blit(sourceTextureId, cameraTarget);
        if (after)
            vivify.RecordBlits(commandBuffer, VivifyRenderOrder.AfterMainEffect, cameraTarget, cameraTarget, descriptor);
        commandBuffer.ReleaseTemporaryRT(sourceTextureId);
    }

    private bool HasMainEffect => (pyramidBloomController != null &&
        (pyramidBloomController.IsReady || pyramidBloomController.IsFadeReady)) ||
        (chromaticAberrationRenderer != null && chromaticAberrationRenderer.IsReady);

    private void RecordMainEffect(CommandBuffer command, RenderTargetIdentifier source,
        RenderTargetIdentifier destination, RenderTextureDescriptor descriptor)
    {
        var bloom = pyramidBloomController != null && pyramidBloomController.IsReady;
        var chromaticAberration = chromaticAberrationRenderer != null && chromaticAberrationRenderer.IsReady;
        var fade = pyramidBloomController != null && pyramidBloomController.IsFadeReady;
        var normalizeBloomSource = bloom && descriptor.graphicsFormat != GraphicsFormat.R16G16B16A16_UNorm;
        if (normalizeBloomSource)
        {
            // Match the normal bloom input contract without clamping Vivify's preceding blits or captures.
            command.GetTemporaryRT(mainEffectSourceId, GetCopyDescriptor(descriptor), FilterMode.Bilinear);
            command.Blit(source, mainEffectSourceId);
            source = new RenderTargetIdentifier(mainEffectSourceId);
        }
        if (bloom && chromaticAberration)
        {
            command.GetTemporaryRT(postBloomOutputId, descriptor, FilterMode.Bilinear);
            pyramidBloomController.RecordRender(command, source, descriptor.width, descriptor.height, postBloomOutputId);
            chromaticAberrationRenderer.RecordRender(command, postBloomOutputId, destination, descriptor.width, descriptor.height);
            command.ReleaseTemporaryRT(postBloomOutputId);
        }
        else if (bloom)
            pyramidBloomController.RecordRender(command, source, descriptor.width, descriptor.height, destination);
        else
        {
            if (chromaticAberration)
                chromaticAberrationRenderer.RecordRender(command, source, destination, descriptor.width, descriptor.height);
            else command.Blit(source, destination);
            if (fade) pyramidBloomController.RecordFade(command, destination);
        }
        if (normalizeBloomSource) command.ReleaseTemporaryRT(mainEffectSourceId);
    }

    private void RenderCaptureMainEffect(RenderTexture source, RenderTexture destination)
    {
        using var command = new CommandBuffer { name = "Vivify Capture Main Effect" };
        RecordMainEffect(command, source, destination, source.descriptor);
        Graphics.ExecuteCommandBuffer(command);
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
        // UNORM clamps light alpha before the bloom gate; 16-bit channels limit added banding.
        descriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
        return descriptor;
    }
}
