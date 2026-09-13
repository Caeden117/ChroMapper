using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public abstract class BloomPrePassBackgroundNonLightRendererCore : BloomPrePassNonLightPass
{
    [SerializeField] public bool KeepDefaultRendering;
    [SerializeField] public bool UseCustomMaterial;
    [SerializeField] public Material CustomMaterial;
    [SerializeField] public bool UseCustomPropertyBlock;
    [SerializeField] public Renderer Renderer;

    private static readonly int worldSpaceCameraPosID = Shader.PropertyToID("_WorldSpaceCameraPos");
    private CommandBuffer commandBuffer;
    private static MaterialPropertyBlock materialPropertyBlock;
    private MaterialPropertyBlock customPropertyBlock;

    public void SetCustomPropertyBlock(MaterialPropertyBlock bloomPropertyBlock)
    {
        customPropertyBlock = bloomPropertyBlock;
        if (materialPropertyBlock == null) materialPropertyBlock = new MaterialPropertyBlock();
    }

    protected virtual void InitIfNeeded()
    {
        if (Renderer == null || !isActiveAndEnabled) return;
        if (!KeepDefaultRendering) Renderer.enabled = false;
        commandBuffer ??= new CommandBuffer { name = "BloomPrePassBackgroundNonLightRenderer" };
    }

    protected virtual void Awake() => InitIfNeeded();

    public override void Render(RenderTexture dest, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        InitIfNeeded();
        var cb = commandBuffer;
        cb.Clear();
        CoreUtils.SetRenderTarget(cb, dest);
        cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        cb.SetGlobalVector(worldSpaceCameraPosID, viewMatrix.GetColumn(3));
        TimeHelper.Instance.SetCommandBufferTimeProperties(cb);

        if (UseCustomPropertyBlock && customPropertyBlock != null)
        {
            Renderer.GetPropertyBlock(materialPropertyBlock);
            Renderer.SetPropertyBlock(customPropertyBlock);
        }

        cb.DrawRenderer(
            Renderer,
            UseCustomMaterial && (bool)CustomMaterial ? CustomMaterial : Renderer.sharedMaterial,
            0,
            0);
        if (UseCustomPropertyBlock && customPropertyBlock != null) Renderer.SetPropertyBlock(materialPropertyBlock);

        Graphics.ExecuteCommandBuffer(commandBuffer);
    }
}
