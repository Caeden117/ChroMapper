using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// Built-in pipeline equivalent of the URP screen-displacement
// passes. Registered displacement renderers are excluded from the normal
// camera pass and drawn after the other transparent objects.
public sealed class ScreenDisplacementRenderingController : MonoBehaviour
{
    private const int maxInstancedRenderers = 1023;
    private const CameraEvent displacementCameraEvent = CameraEvent.AfterForwardAlpha;

    private static readonly int grabTextureId = Shader.PropertyToID("_ScreenDisplacementGrabTexture");
    private static readonly int grabTextureTexelSizeId =
        Shader.PropertyToID("_ScreenDisplacementGrabTexture_TexelSize");
    [SerializeField, Range(0, 31)] private int displacementLayer = 31;

    private readonly List<ScreenDisplacementRenderer> sortedRenderers = new();
    private readonly Matrix4x4[] instanceMatrices = new Matrix4x4[maxInstancedRenderers];
    private readonly Vector4[] instanceColors = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceTintColors = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceAddColors = new Vector4[maxInstancedRenderers];
    private readonly float[] instanceCutouts = new float[maxInstancedRenderers];
    private readonly Vector4[] instanceCutoutTexOffsets = new Vector4[maxInstancedRenderers];
    private readonly Vector4[] instanceUvScales = new Vector4[maxInstancedRenderers];
    private MaterialPropertyBlock sourcePropertyBlock;
    private MaterialPropertyBlock instancedPropertyBlock;

    private static readonly int colorId = Shader.PropertyToID("_Color");
    private static readonly int tintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int addColorId = Shader.PropertyToID("_AddColor");
    private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
    private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
    private static readonly int uvScaleId = Shader.PropertyToID("_UVScale");

    private Camera activeCamera;
    private CommandBuffer commandBuffer;
    private int previousDisplacementLayerMask;
    private bool active;
    private bool commandBufferAttached;
    private bool screenDisplacementEnabled;
    private bool settingsCallbackSubscribed;

    public void AssignToCamera(CameraController cameraController)
    {
        DetachCommandBuffer();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        AttachCommandBuffer();
    }

    private void OnEnable()
    {
        UpdateScreenDisplacement(Settings.Instance.ScreenDisplacement);
        if (!settingsCallbackSubscribed)
        {
            Settings.NotifyBySettingName(nameof(Settings.ScreenDisplacement), UpdateScreenDisplacement);
            settingsCallbackSubscribed = true;
        }
        if (screenDisplacementEnabled) Activate();
    }

    private void OnDisable()
    {
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(
                nameof(Settings.ScreenDisplacement), UpdateScreenDisplacement);
            settingsCallbackSubscribed = false;
        }
        Deactivate();
        ScreenDisplacementRenderer.SetEnabled(false);
    }

    private void OnDestroy()
    {
        OnDisable();
        Deactivate();
        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }
    }

    private void Activate()
    {
        if (active || !screenDisplacementEnabled) return;
        active = true;
        ScreenDisplacementRenderer.SetEnabled(true);
        Camera.onPreRender += OnCameraPreRender;
        AttachCommandBuffer();
    }

    private void Deactivate()
    {
        if (active)
        {
            active = false;
            Camera.onPreRender -= OnCameraPreRender;
        }
        DetachCommandBuffer();
        ScreenDisplacementRenderer.SetEnabled(false);
    }

    private void AttachCommandBuffer()
    {
        if (!active || activeCamera == null || commandBufferAttached) return;

        commandBuffer ??= new CommandBuffer { name = "ChroMapper Screen Displacement" };
        var layerBit = 1 << displacementLayer;
        previousDisplacementLayerMask = activeCamera.cullingMask & layerBit;
        activeCamera.cullingMask &= ~layerBit;
        activeCamera.AddCommandBuffer(displacementCameraEvent, commandBuffer);
        commandBufferAttached = true;
    }

    private void DetachCommandBuffer()
    {
        if (!commandBufferAttached) return;

        if (activeCamera != null)
        {
            activeCamera.RemoveCommandBuffer(displacementCameraEvent, commandBuffer);
            var layerBit = 1 << displacementLayer;
            activeCamera.cullingMask =
                (activeCamera.cullingMask & ~layerBit) | previousDisplacementLayerMask;
        }

        commandBufferAttached = false;
        commandBuffer?.Clear();
        Shader.SetGlobalTexture(grabTextureId, null);
        Shader.SetGlobalVector(grabTextureTexelSizeId, Vector4.zero);
    }

    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera || commandBuffer == null) return;

        commandBuffer.Clear();
        CollectRenderers();
        if (sortedRenderers.Count == 0) return;

        var grabDescriptor = GetGrabDescriptor();
        var sourceWidth = grabDescriptor.width;
        var sourceHeight = grabDescriptor.height;
        var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

        commandBuffer.GetTemporaryRT(grabTextureId, grabDescriptor, FilterMode.Bilinear);
        commandBuffer.Blit(cameraTarget, grabTextureId);
        commandBuffer.SetGlobalTexture(grabTextureId, grabTextureId);
        commandBuffer.SetGlobalVector(
            grabTextureTexelSizeId,
            new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight));
        commandBuffer.SetRenderTarget(cameraTarget);

        DrawRenderers();

        commandBuffer.SetGlobalTexture(grabTextureId, Texture2D.blackTexture);
        commandBuffer.SetGlobalVector(grabTextureTexelSizeId, Vector4.zero);
        commandBuffer.ReleaseTemporaryRT(grabTextureId);
    }

    private void DrawRenderers()
    {
        // Existing MonoBehaviours can survive a script reload without running new
        // field initializers. Initialize the native-backed blocks at their use boundary.
        sourcePropertyBlock ??= new MaterialPropertyBlock();
        instancedPropertyBlock ??= new MaterialPropertyBlock();

        var instanceCount = 0;
        Mesh batchMesh = null;
        Material batchMaterial = null;
        var batchSortingLayer = 0;
        var batchSortingOrder = 0;
        var batchRenderQueue = 0;

        foreach (var displacementRenderer in sortedRenderers)
        {
            var renderer = displacementRenderer.TargetRenderer;
            var material = renderer.sharedMaterial;
            if (!TryGetInstancingMesh(renderer, material, out var mesh))
            {
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
                commandBuffer.DrawRenderer(renderer, material, 0, -1);
                continue;
            }

            var compatible = instanceCount == 0
                || (mesh == batchMesh
                    && material == batchMaterial
                    && renderer.sortingLayerID == batchSortingLayer
                    && renderer.sortingOrder == batchSortingOrder
                    && material.renderQueue == batchRenderQueue);
            if (!compatible)
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);

            if (instanceCount == 0)
            {
                batchMesh = mesh;
                batchMaterial = material;
                batchSortingLayer = renderer.sortingLayerID;
                batchSortingOrder = renderer.sortingOrder;
                batchRenderQueue = material.renderQueue;
            }

            AddInstancedRenderer(renderer, material, instanceCount++);
            if (instanceCount == maxInstancedRenderers)
                FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
        }

        FlushInstancedBatch(ref instanceCount, ref batchMesh, ref batchMaterial);
    }

    private bool TryGetInstancingMesh(Renderer renderer, Material material, out Mesh mesh)
    {
        mesh = null;
        if (!SystemInfo.supportsInstancing || material == null || !material.enableInstancing)
            return false;
        if (renderer is not MeshRenderer) return false;

        var meshFilter = renderer.GetComponent<MeshFilter>();
        mesh = meshFilter == null ? null : meshFilter.sharedMesh;
        return mesh != null && mesh.subMeshCount > 0;
    }

    private void AddInstancedRenderer(Renderer renderer, Material material, int index)
    {
        if (index >= maxInstancedRenderers)
            throw new System.ArgumentOutOfRangeException(nameof(index));

        instanceMatrices[index] = renderer.transform.localToWorldMatrix;
        sourcePropertyBlock.Clear();
        renderer.GetPropertyBlock(sourcePropertyBlock);
        // Preserve the active-space RGB stored by SetColor when uploading raw vector arrays.
        instanceColors[index] = sourcePropertyBlock.HasProperty(colorId)
            ? sourcePropertyBlock.GetVector(colorId) : (Vector4)material.GetColor(colorId);
        instanceTintColors[index] = sourcePropertyBlock.HasProperty(tintColorId)
            ? sourcePropertyBlock.GetVector(tintColorId) : (Vector4)material.GetColor(tintColorId);
        instanceAddColors[index] = sourcePropertyBlock.HasProperty(addColorId)
            ? sourcePropertyBlock.GetVector(addColorId) : (Vector4)material.GetColor(addColorId);
        instanceCutouts[index] = sourcePropertyBlock.HasProperty(cutoutId)
            ? sourcePropertyBlock.GetFloat(cutoutId) : material.GetFloat(cutoutId);
        instanceCutoutTexOffsets[index] = sourcePropertyBlock.HasProperty(cutoutTexOffsetId)
            ? sourcePropertyBlock.GetVector(cutoutTexOffsetId) : material.GetVector(cutoutTexOffsetId);
        instanceUvScales[index] = sourcePropertyBlock.HasProperty(uvScaleId)
            ? sourcePropertyBlock.GetVector(uvScaleId) : material.GetVector(uvScaleId);

        // DrawRenderer broke GPU batching; carry MPB values as instanced arrays instead.
    }

    private void FlushInstancedBatch(
        ref int instanceCount, ref Mesh batchMesh, ref Material batchMaterial)
    {
        if (instanceCount == 0) return;
        if (instanceCount > maxInstancedRenderers)
            throw new System.InvalidOperationException("Screen displacement instance batch exceeds Unity's 1023 limit.");

        instancedPropertyBlock.Clear();
        instancedPropertyBlock.SetVectorArray(colorId, instanceColors);
        instancedPropertyBlock.SetVectorArray(tintColorId, instanceTintColors);
        instancedPropertyBlock.SetVectorArray(addColorId, instanceAddColors);
        instancedPropertyBlock.SetFloatArray(cutoutId, instanceCutouts);
        instancedPropertyBlock.SetVectorArray(cutoutTexOffsetId, instanceCutoutTexOffsets);
        instancedPropertyBlock.SetVectorArray(uvScaleId, instanceUvScales);
        commandBuffer.DrawMeshInstanced(
            batchMesh, 0, batchMaterial, -1, instanceMatrices, instanceCount, instancedPropertyBlock);
        instanceCount = 0;
        batchMesh = null;
        batchMaterial = null;
    }

    private void CollectRenderers()
    {
        sortedRenderers.Clear();
        foreach (var displacementRenderer in ScreenDisplacementRenderer.Renderers)
        {
            if (displacementRenderer != null && displacementRenderer.IsReady)
                sortedRenderers.Add(displacementRenderer);
        }

        var cameraTransform = activeCamera.transform;
        sortedRenderers.Sort((left, right) =>
        {
            var leftRenderer = left.TargetRenderer;
            var rightRenderer = right.TargetRenderer;
            var comparison = SortingLayer.GetLayerValueFromID(leftRenderer.sortingLayerID)
                .CompareTo(SortingLayer.GetLayerValueFromID(rightRenderer.sortingLayerID));
            if (comparison != 0) return comparison;

            comparison = leftRenderer.sortingOrder.CompareTo(rightRenderer.sortingOrder);
            if (comparison != 0) return comparison;

            comparison = leftRenderer.sharedMaterial.renderQueue
                .CompareTo(rightRenderer.sharedMaterial.renderQueue);
            if (comparison != 0) return comparison;

            // Approximate the game's QuantizedFrontToBack criterion. The built-in
            // command buffer does not expose URP's renderer-list sorter.
            var leftDepth = Vector3.Dot(
                leftRenderer.bounds.center - cameraTransform.position,
                cameraTransform.forward);
            var rightDepth = Vector3.Dot(
                rightRenderer.bounds.center - cameraTransform.position,
                cameraTransform.forward);
            return leftDepth.CompareTo(rightDepth);
        });
    }

    private void UpdateScreenDisplacement(object value)
    {
        screenDisplacementEnabled = System.Convert.ToBoolean(value);
        if (screenDisplacementEnabled)
        {
            ScreenDisplacementRenderer.SetEnabled(true);
            if (isActiveAndEnabled) Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private RenderTextureDescriptor GetGrabDescriptor()
    {
        var descriptor = GetCameraTargetDescriptor();
        descriptor.depthBufferBits = 0;
        descriptor.depthStencilFormat = GraphicsFormat.None;
        descriptor.msaaSamples = 1;
        descriptor.useDynamicScale = false;
        descriptor.useDynamicScaleExplicit = false;
        return descriptor;
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

}
