using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "BloomfogRendererSO", menuName = "Environment/BloomfogRendererSO")]
public class BloomfogRendererSO : ScriptableObject
{
    private static readonly int vertexTransformMatrix = Shader.PropertyToID("_VertexTransformMatrix");

    private const int startCapacity = 2048;

    private BloomfogQuad[] bloomfogQuads;
    private BloomfogVertex[] bloomfogVertices;
    private static readonly int customFogTextureToScreenRatio =
        Shader.PropertyToID("_CustomFogTextureToScreenRatio");
    private static readonly int stereoCameraEyeOffsets =
        Shader.PropertyToID("_StereoCameraEyeOffsets");

    // Independent horizontal and vertical fog-frustum angles, in degrees.
    public Vector2 FOV = new(130f, 130f);
    public float LineWidth = 0.02f;
    public Material BloomfogObjectMaterial;

    private int capacity = startCapacity;
    private CommandBuffer bloomfogCommandBuffer;
    private Mesh bloomfogMesh;
    private readonly List<LightBatch> lightBatches = new();
    private SubMeshDescriptor[] subMeshDescriptors;
    private int configuredBatchCount;
    private bool subMeshDescriptorsDirty = true;
    private int activeBatchCount;
    private Matrix4x4 renderedViewMatrix;
    private Matrix4x4 renderedProjectionMatrix;
    private Vector2 renderedTextureToScreenRatio;
    private Vector2 renderedEyeOffsets;
    private bool hasRenderedMatrices;

    public void Initialize()
    {
        if (bloomfogCommandBuffer == null)
            bloomfogCommandBuffer = new CommandBuffer() { name = "Bloomfog Render" };

        if (bloomfogMesh == null)
            PrepareMesh(true);
        Shader.SetGlobalMatrix(vertexTransformMatrix, Matrix4x4.Ortho(0, 1, 1, 0, -1, 1));
    }

    private void OnDisable() => Release();

    /// <summary>Releases this renderer's shared mesh, command buffer, and CPU buffers.</summary>
    /// <remarks>Individual camera controllers borrow these resources and must not release them.
    /// The next initialization recreates them.</remarks>
    public void Release()
    {
        if (bloomfogMesh != null)
        {
            bloomfogMesh.Clear();
            if (Application.isPlaying) Destroy(bloomfogMesh);
            else DestroyImmediate(bloomfogMesh);
            bloomfogMesh = null;
        }
        if (bloomfogCommandBuffer != null)
        {
            bloomfogCommandBuffer.Release();
            bloomfogCommandBuffer = null;
        }
        ClearLightBatches();
        bloomfogQuads = null;
        bloomfogVertices = null;
        hasRenderedMatrices = false;
    }

    public void RenderToTexture(Camera camera, RenderTexture tex, out Vector2 textureToScreenRatio)
    {
        var projectionMatrix = camera.projectionMatrix;
        var eyeOffsets = Vector2.zero;
        if (camera.stereoEnabled)
        {
            var leftProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            var rightProjectionMatrix = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            projectionMatrix = leftProjectionMatrix;
            for (var i = 0; i < 16; i++)
                projectionMatrix[i] = Mathf.Lerp(leftProjectionMatrix[i], rightProjectionMatrix[i], 0.5f);
            var t = -(leftProjectionMatrix.m02 - rightProjectionMatrix.m02) * 0.25f;
            eyeOffsets = new Vector2(-t, t);
        }

        RenderToTextureInternal(
            camera.worldToCameraMatrix,
            projectionMatrix,
            tex,
            out textureToScreenRatio,
            eyeOffsets);
    }

    public void RenderToTexture(
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix,
        RenderTexture tex,
        out Vector2 textureToScreenRatio)
    {
        RenderToTextureInternal(
            viewMatrix,
            projectionMatrix,
            tex,
            out textureToScreenRatio,
            Vector2.zero);
    }

    private void RenderToTextureInternal(
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix,
        RenderTexture tex,
        out Vector2 textureToScreenRatio,
        Vector2 eyeOffsets)
    {
        if (bloomfogCommandBuffer == null || bloomfogMesh == null) Initialize();
        Shader.SetGlobalVector(stereoCameraEyeOffsets, Vector2.zero);
        Shader.SetGlobalVector(customFogTextureToScreenRatio, Vector2.one);

        // Crop the camera projection to the configured fog frustum.
        textureToScreenRatio.x = Mathf.Clamp01(
            1f / (Mathf.Tan(FOV.x * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m00));
        textureToScreenRatio.y = Mathf.Clamp01(
            1f / (Mathf.Tan(FOV.y * 0.5f * Mathf.Deg2Rad) * projectionMatrix.m11));
        projectionMatrix.m00 *= textureToScreenRatio.x;
        projectionMatrix.m02 *= textureToScreenRatio.x;
        projectionMatrix.m11 *= textureToScreenRatio.y;
        projectionMatrix.m12 *= textureToScreenRatio.y;

        bloomfogCommandBuffer.Clear();
        bloomfogCommandBuffer.SetRenderTarget(tex);
        bloomfogCommandBuffer.ClearRenderTarget(true, true, Color.clear);

        RenderQuads(viewMatrix, projectionMatrix, LineWidth);

        for (var i = 0; i < activeBatchCount; i++)
        {
            var batch = lightBatches[i];
            if (batch.LightCount > 0 && batch.Material != null)
                bloomfogCommandBuffer.DrawMesh(bloomfogMesh, Matrix4x4.identity, batch.Material, i);
        }

        Graphics.ExecuteCommandBuffer(bloomfogCommandBuffer);

        // Light quads use the adjusted projection. Non-light phases use the API-corrected Y orientation.
        if (!SystemInfo.usesReversedZBuffer)
        {
            projectionMatrix.m11 *= -1f;
            projectionMatrix.m12 *= -1f;
        }

        renderedViewMatrix = viewMatrix;
        renderedProjectionMatrix = projectionMatrix;
        renderedTextureToScreenRatio = textureToScreenRatio;
        renderedEyeOffsets = eyeOffsets;
        hasRenderedMatrices = true;

        foreach (var bloomPrePassBeforeBlur in BloomPrePassNonLightPass.BloomPrePassBeforeBlurList)
        {
            bloomPrePassBeforeBlur.Render(tex, viewMatrix, projectionMatrix);
        }
    }

    public void RenderAfterBlur(RenderTexture tex)
    {
        if (!hasRenderedMatrices || tex == null) return;

        foreach (var bloomPrePassAfterBlur in BloomPrePassNonLightPass.BloomPrePassAfterBlurList)
        {
            bloomPrePassAfterBlur.Render(tex, renderedViewMatrix, renderedProjectionMatrix);
        }
    }

    internal void PublishGlobals()
    {
        if (!hasRenderedMatrices) return;
        Shader.SetGlobalVector(customFogTextureToScreenRatio, renderedTextureToScreenRatio);
        Shader.SetGlobalVector(stereoCameraEyeOffsets, renderedEyeOffsets);
    }

    private void RenderQuads(Matrix4x4 view, Matrix4x4 projection, float lineWidth)
    {
        var lights = BloomFogObject.AllBloomFogLights;

        if (lights.Count > capacity) PrepareMesh();

        BuildLightBatches(lights);
        if (activeBatchCount == 0)
            return;

        var activeLights = 0;
        for (var batchIndex = 0; batchIndex < activeBatchCount; batchIndex++)
        {
            var batch = lightBatches[batchIndex];
            var firstLight = activeLights;
            for (var lightIndex = 0; lightIndex < batch.Lights.Count; lightIndex++)
                batch.Lights[lightIndex].ApplyToQuad(
                    ref activeLights,
                    bloomfogQuads,
                    view,
                    projection,
                    lineWidth);
            batch.FirstLight = firstLight;
            batch.LightCount = activeLights - firstLight;
        }

        for (var i = 0; i < activeLights; i++)
            bloomfogQuads[i].CopyVerticesTo(bloomfogVertices, i * 4);

        bloomfogMesh.SetVertexBufferData(
            bloomfogVertices,
            0,
            0,
            activeLights * 4,
            0,
            MeshUpdateFlags.DontRecalculateBounds);
        var descriptorsChanged = subMeshDescriptorsDirty;
        if (subMeshDescriptors == null || subMeshDescriptors.Length < activeBatchCount)
        {
            System.Array.Resize(ref subMeshDescriptors, activeBatchCount);
            descriptorsChanged = true;
        }
        for (var i = 0; i < activeBatchCount; i++)
        {
            var batch = lightBatches[i];
            var descriptor = new SubMeshDescriptor(batch.FirstLight * 6, batch.LightCount * 6)
            {
                firstVertex = batch.FirstLight * 4,
                vertexCount = batch.LightCount * 4,
            };
            var previous = subMeshDescriptors[i];
            if (previous.indexStart == descriptor.indexStart
                && previous.indexCount == descriptor.indexCount
                && previous.firstVertex == descriptor.firstVertex
                && previous.vertexCount == descriptor.vertexCount)
                continue;
            subMeshDescriptors[i] = descriptor;
            descriptorsChanged = true;
        }
        for (var i = activeBatchCount; i < configuredBatchCount; i++)
        {
            subMeshDescriptors[i] = new SubMeshDescriptor(0, 0);
            descriptorsChanged = true;
        }
        configuredBatchCount = activeBatchCount;
        if (descriptorsChanged)
        {
            bloomfogMesh.SetSubMeshes(subMeshDescriptors, MeshUpdateFlags.DontRecalculateBounds);
            subMeshDescriptorsDirty = false;
        }
    }

    private void BuildLightBatches(List<BloomFogObject> lights)
    {
        ClearLightBatches();

        for (var i = 0; i < lights.Count; i++)
        {
            var light = lights[i];
            var material = light.LightType?.Material ?? BloomfogObjectMaterial;
            var renderingPriority = light.LightType?.RenderingPriority ?? 0;
            var batchIndex = FindBatch(material, renderingPriority);
            if (batchIndex < 0) batchIndex = InsertBatch(material, renderingPriority);
            lightBatches[batchIndex].Lights.Add(light);
        }
    }

    private int FindBatch(Material material, int renderingPriority)
    {
        for (var i = 0; i < activeBatchCount; i++)
        {
            var batch = lightBatches[i];
            if (batch.Material == material && batch.RenderingPriority == renderingPriority) return i;
        }
        return -1;
    }

    private int InsertBatch(Material material, int renderingPriority)
    {
        var batch = activeBatchCount < lightBatches.Count ? lightBatches[activeBatchCount] : new LightBatch();
        if (activeBatchCount == lightBatches.Count) lightBatches.Add(batch);

        var insertIndex = activeBatchCount;
        while (insertIndex > 0 && lightBatches[insertIndex - 1].RenderingPriority > renderingPriority)
        {
            lightBatches[insertIndex] = lightBatches[insertIndex - 1];
            insertIndex--;
        }
        lightBatches[insertIndex] = batch;
        batch.Material = material;
        batch.RenderingPriority = renderingPriority;
        activeBatchCount++;
        return insertIndex;
    }

    private void ClearLightBatches()
    {
        for (var i = 0; i < lightBatches.Count; i++)
        {
            var batch = lightBatches[i];
            batch.Lights.Clear();
            batch.Material = null;
            batch.RenderingPriority = 0;
            batch.FirstLight = 0;
            batch.LightCount = 0;
        }
        activeBatchCount = 0;
    }

    private void PrepareMesh(bool force = false)
    {
        var lightCount = BloomFogObject.AllBloomFogLights.Count;

        if (!force && bloomfogMesh != null && capacity >= lightCount) return;

        while (capacity < lightCount)
        {
            capacity *= 2;
        }

        if (bloomfogMesh != null)
        {
            Debug.LogWarning("Need to recreate bloomfog mesh with larger capacity: " + capacity);
            bloomfogMesh.Clear();
        }
        else
        {
            Debug.Log("Generating bloomfog mesh with capacity: " + capacity);
            bloomfogMesh = new Mesh
            {
                name = "Bloomfog Mesh",
                indexFormat = IndexFormat.UInt32,
                vertexBufferTarget = GraphicsBuffer.Target.Vertex | GraphicsBuffer.Target.Raw
            };
        }

        // All vertex attributes share one interleaved stream matching BloomfogVertex.
        var vertexAttributes = new VertexAttributeDescriptor[]
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, 0),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 0),
        };
        bloomfogMesh.SetVertexBufferParams(4 * capacity, vertexAttributes);

        bloomfogQuads = new BloomfogQuad[capacity];
        bloomfogVertices = new BloomfogVertex[capacity * 4];
        // Allocation does not initialize the native buffer. Upload the zeroed array once;
        // subsequent frames update only active vertices, leaving unused capacity finite.
        bloomfogMesh.SetVertexBufferData(
            bloomfogVertices, 0, 0, bloomfogVertices.Length, 0,
            MeshUpdateFlags.DontRecalculateBounds);

        // Indices are immutable; each four-vertex block forms one quad.
        var indexCount = capacity * 6;
        var data = new NativeArray<ushort>(indexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        try
        {
            for (var i = 0; i < capacity; i++)
            {
                data[i * 6] = (ushort)(i * 4);
                data[(i * 6) + 1] = (ushort)((i * 4) + 1);
                data[(i * 6) + 2] = (ushort)((i * 4) + 2);
                data[(i * 6) + 3] = (ushort)((i * 4) + 2);
                data[(i * 6) + 4] = (ushort)((i * 4) + 3);
                data[(i * 6) + 5] = (ushort)(i * 4);
            }
            bloomfogMesh.SetIndexBufferParams(data.Length, IndexFormat.UInt16);
            bloomfogMesh.SetIndexBufferData(data, 0, 0, data.Length, MeshUpdateFlags.Default);
        }
        finally
        {
            data.Dispose();
        }

        // RenderQuads creates one active submesh for each material-and-priority batch.
        bloomfogMesh.subMeshCount = 1;
        bloomfogMesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), MeshUpdateFlags.DontRecalculateBounds);
        subMeshDescriptorsDirty = true;
        bloomfogMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        bloomfogMesh.UploadMeshData(false);
    }

    private sealed class LightBatch
    {
        public readonly List<BloomFogObject> Lights = new();
        public Material Material;
        public int RenderingPriority;
        public int FirstLight;
        public int LightCount;
    }
}
