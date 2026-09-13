using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BloomPrePassBackgroundNonLightInstancedGroupRenderer : BloomPrePassNonLightPass
{
    [SerializeField] public BloomPrePassBackgroundNonLightRenderer[] Renderers;
    [SerializeField] public SupportedProperty[] SupportedProperties;

    private static readonly int worldSpaceCameraPosID = Shader.PropertyToID("_WorldSpaceCameraPos");
    private const string internalMatricesCachingId = "INTERNAL_MATRICES";
    private readonly Dictionary<string, float[]> reusableFloatArrays = new();
    private readonly Dictionary<string, Vector4[]> reusableVectorArrays = new();
    private readonly Dictionary<string, Matrix4x4[]> reusableMatrixArrays = new();

    private int reusableArraysSize;
    private CommandBuffer commandBuffer;
    private MaterialPropertyBlock reusableSetMaterialPropertyBlock;
    private MaterialPropertyBlock reusableGetMaterialPropertyBlock;

    protected void Awake() => InitIfNeeded();

    private void InitIfNeeded()
    {
        commandBuffer ??= new CommandBuffer { name = "BloomPrePassBackgroundNonLightInstancedRenderer" };

        var supportedProperties = SupportedProperties;
        foreach (var obj in supportedProperties) obj.PropertyId = Shader.PropertyToID(obj.PropertyName);

        var renderers = Renderers;
        foreach (var t in renderers) t.IsPartOfInstancedRendering = true;

        if (reusableArraysSize != Renderers.Length)
        {
            reusableFloatArrays.Clear();
            reusableVectorArrays.Clear();
            reusableMatrixArrays.Clear();
            reusableArraysSize = Renderers.Length;
            reusableSetMaterialPropertyBlock = new MaterialPropertyBlock();
            reusableGetMaterialPropertyBlock = new MaterialPropertyBlock();
        }

        if (reusableGetMaterialPropertyBlock == null)
        {
            reusableSetMaterialPropertyBlock = new MaterialPropertyBlock();
            reusableGetMaterialPropertyBlock = new MaterialPropertyBlock();
        }
    }

    public override void Render(RenderTexture dest, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        var cb = commandBuffer;
        cb.Clear();
        if (Renderers.Length == 0) return;

        var sharedMesh = Renderers[0].MeshFilter.sharedMesh;
        var material = Renderers[0].UseCustomMaterial
            ? Renderers[0].CustomMaterial
            : Renderers[0].Renderer.sharedMaterial;
        cb.SetRenderTarget(dest);
        cb.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        cb.SetGlobalVector(worldSpaceCameraPosID, viewMatrix.GetColumn(3));
        TimeHelper.Instance.SetCommandBufferTimeProperties(cb);

        if (Renderers.Length == 1)
        {
            Debug.LogWarning(
                "Using BloomPrePassBackgroundNonLightInstancedRenderingSystem to render single Renderer, this add extra overhead with no benefit");
            cb.DrawRenderer(Renderers[0].Renderer, material, 0, 0);
        }
        else
        {
            var cachedMatrixArray = GetCachedMatrixArray(internalMatricesCachingId);
            SupportedProperty[] supportedProperties;
            for (var i = 0; i < Renderers.Length; i++)
            {
                var bloomPrePassBackgroundNonLightRenderer = Renderers[i];
                if (bloomPrePassBackgroundNonLightRenderer.isActiveAndEnabled)
                    cachedMatrixArray[i] = bloomPrePassBackgroundNonLightRenderer.CachedTransform.localToWorldMatrix;
                else
                    cachedMatrixArray[i] = Matrix4x4.zero;

                bloomPrePassBackgroundNonLightRenderer.Renderer.GetPropertyBlock(reusableGetMaterialPropertyBlock);
                supportedProperties = SupportedProperties;
                foreach (var supportedProperty in supportedProperties)
                {
                    switch (supportedProperty.PropertyType)
                    {
                        case PropertyType.Vector:
                            GetCachedVectorArray(supportedProperty.PropertyName)[i] =
                                reusableGetMaterialPropertyBlock.GetVector(supportedProperty.PropertyId);
                            break;
                        case PropertyType.Color:
                            GetCachedVectorArray(supportedProperty.PropertyName)[i] =
                                reusableGetMaterialPropertyBlock.GetColor(supportedProperty.PropertyId);
                            break;
                        case PropertyType.Matrix4X4:
                            GetCachedMatrixArray(supportedProperty.PropertyName)[i] =
                                reusableGetMaterialPropertyBlock.GetMatrix(supportedProperty.PropertyId);
                            break;
                        case PropertyType.Float:
                            GetCachedFloatArray(supportedProperty.PropertyName)[i] =
                                reusableGetMaterialPropertyBlock.GetFloat(supportedProperty.PropertyId);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            supportedProperties = SupportedProperties;
            foreach (var supportedProperty2 in supportedProperties)
            {
                switch (supportedProperty2.PropertyType)
                {
                    case PropertyType.Vector:
                    case PropertyType.Color:
                        {
                            var cachedVectorArray = GetCachedVectorArray(supportedProperty2.PropertyName);
                            reusableSetMaterialPropertyBlock.SetVectorArray(
                                supportedProperty2.PropertyId,
                                cachedVectorArray);
                            break;
                        }
                    case PropertyType.Matrix4X4:
                        {
                            var cachedMatrixArray2 = GetCachedMatrixArray(supportedProperty2.PropertyName);
                            reusableSetMaterialPropertyBlock.SetMatrixArray(
                                supportedProperty2.PropertyId,
                                cachedMatrixArray2);
                            break;
                        }
                    case PropertyType.Float:
                        {
                            var cachedFloatArray = GetCachedFloatArray(supportedProperty2.PropertyName);
                            reusableSetMaterialPropertyBlock.SetFloatArray(
                                supportedProperty2.PropertyId,
                                cachedFloatArray);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            cb.DrawMeshInstanced(
                sharedMesh,
                0,
                material,
                0,
                cachedMatrixArray,
                Renderers.Length,
                reusableSetMaterialPropertyBlock);
        }

        Graphics.ExecuteCommandBuffer(cb);
    }

    private Matrix4x4[] GetCachedMatrixArray(string propertyName)
    {
        if (reusableMatrixArrays.TryGetValue(propertyName, out var value)) return value;
        return reusableMatrixArrays[propertyName] = new Matrix4x4[Renderers.Length];
    }

    private float[] GetCachedFloatArray(string propertyName)
    {
        if (reusableFloatArrays.TryGetValue(propertyName, out var value)) return value;
        return reusableFloatArrays[propertyName] = new float[Renderers.Length];
    }

    private Vector4[] GetCachedVectorArray(string propertyName)
    {
        if (reusableVectorArrays.TryGetValue(propertyName, out var value)) return value;
        return reusableVectorArrays[propertyName] = new Vector4[Renderers.Length];
    }

    [ContextMenu("AutoFill Renderers")]
    private void AutoFillRenderers() => Renderers = GetComponentsInChildren<BloomPrePassBackgroundNonLightRenderer>();

    [Serializable]
    public class SupportedProperty
    {
        public PropertyType PropertyType;
        public string PropertyName;
        [NonSerialized] public int PropertyId;
    }

    public enum PropertyType
    {
        Float,
        Vector,
        Color,
        Matrix4X4
    }
}
