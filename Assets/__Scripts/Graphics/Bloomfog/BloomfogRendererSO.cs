using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "BloomfogRendererSO", menuName = "Environment/BloomfogRendererSO")]
public class BloomfogRendererSO : ScriptableObject
{
    private static readonly int vertexTransformMatrix = Shader.PropertyToID("_VertexTransformMatrix");

    private const int startCapacity = 2048;

    private static BloomfogQuad[] bloomfogQuads = new BloomfogQuad[startCapacity];
    private static BloomfogVertex[] bloomfogVertices = new BloomfogVertex[startCapacity * 4];
    private static readonly int customFogTextureToScreenRatio =
        Shader.PropertyToID("_CustomFogTextureToScreenRatio");
    private static readonly int stereoCameraEyeOffsets =
        Shader.PropertyToID("_StereoCameraEyeOffsets");

    // Recovered serialized HD and LD bloom-prepass value.
    public Vector2 FOV = new(130f, 130f);
    public float LineWidth = 0.02f;
    public Material BloomfogObjectMaterial;

    private int capacity = startCapacity;
    private CommandBuffer bloomfogCommandBuffer;
    private Mesh bloomfogMesh;
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

        // Adjust projection matrix to account for FOV
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

        bloomfogCommandBuffer.DrawMesh(bloomfogMesh, Matrix4x4.identity, BloomfogObjectMaterial);

        Graphics.ExecuteCommandBuffer(bloomfogCommandBuffer);

        // Beat Saber renders the light geometry with the original adjusted
        // projection, then flips the non-reversed-Z projection for non-light
        // prepass objects. Keep the flipped matrices for both list phases.
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

        var activeLights = 0;
        for (var i = 0; i < lights.Count; i++)
        {
            lights[i].ApplyToQuad(ref activeLights, bloomfogQuads, view, projection, lineWidth);
        }

        var descriptor = new SubMeshDescriptor(0, activeLights * 6)
        {
            firstVertex = 0,
            vertexCount = activeLights * 4,
        };

        for (var i = 0; i < activeLights; i++)
            bloomfogQuads[i].CopyVerticesTo(bloomfogVertices, i * 4);

        bloomfogMesh.SetVertexBufferData(
            bloomfogVertices,
            0,
            0,
            activeLights * 4,
            0,
            MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.subMeshCount = 1;
        bloomfogMesh.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.UploadMeshData(false);
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

        // Initialize vertex buffer
        var vertexAttributes = new VertexAttributeDescriptor[]
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 3, 0),
            new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, 0),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3, 0),
        };
        bloomfogMesh.SetVertexBufferParams(4 * capacity, vertexAttributes);

        // Recreate quad array (should be initialized to zeroes by default)
        bloomfogQuads = new BloomfogQuad[capacity];
        bloomfogVertices = new BloomfogVertex[capacity * 4];

        // Initialize index buffer
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

        // Set submesh and bounds
        bloomfogMesh.subMeshCount = 1;
        bloomfogMesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount), MeshUpdateFlags.DontRecalculateBounds);
        bloomfogMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        bloomfogMesh.UploadMeshData(false);
    }
}
