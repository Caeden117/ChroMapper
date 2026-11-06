using UnityEngine;
using UnityEngine.Rendering;

public static class BloomRenderUtility
{
    public const int MaxPyramidSize = 16;

    // Renderer field initializers also call this utility, when Unity forbids device queries.
    // Keep format initialization separate so the query first runs when a render pass requests it.
    private static class CachedBloomFormat
    {
        internal static readonly RenderTextureFormat Value =
            SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)
                ? RenderTextureFormat.RGB111110Float
                : RenderTextureFormat.ARGBHalf;
    }

    public static RenderTextureFormat GetBloomTextureFormat() => CachedBloomFormat.Value;

    public static RenderTextureDescriptor CreateDescriptor(
        int width, int height, RenderTextureFormat format)
    {
        return new RenderTextureDescriptor(width, height, format, 0)
        {
            volumeDepth = 1,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2D,
            depthBufferBits = 0,
            sRGB = false,
            useMipMap = false,
            autoGenerateMips = false,
            enableRandomWrite = false
        };
    }

    /// <summary>Acquires a bilinear, clamped temporary texture.</summary>
    /// <remarks>The caller must return it with <see cref="RenderTexture.ReleaseTemporary(RenderTexture)"/>.</remarks>
    public static RenderTexture GetTemporary(RenderTextureDescriptor descriptor)
    {
        var texture = RenderTexture.GetTemporary(descriptor);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    public static Vector4 GetTexelSize(Texture texture) =>
        new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height);

    public static RenderTextureDescriptor[] BuildPyramidDescriptors(
        int width, int height, int count, RenderTextureFormat format)
    {
        var descriptors = new RenderTextureDescriptor[count];
        for (var i = 0; i < count; i++)
        {
            descriptors[i] = CreateDescriptor(width, height, format);
            width = Mathf.Max(width / 2, 1);
            height = Mathf.Max(height / 2, 1);
        }

        return descriptors;
    }

    public static int[] CreateTextureIds(string prefix)
    {
        var ids = new int[MaxPyramidSize];
        for (var i = 0; i < ids.Length; i++) ids[i] = Shader.PropertyToID(prefix + i);
        return ids;
    }

    /// <summary>Calculates the bounded level count and fractional sampling scale for a radius.</summary>
    public static void CalculatePyramidParameters(
        int width, int height, float radius, out int levelCount, out float sampleScale)
    {
        var logs = Mathf.Log(Mathf.Max(width, height), 2f)
                   + Mathf.Min(radius, 10f) - 10f;
        var logsI = Mathf.FloorToInt(logs);
        levelCount = Mathf.Clamp(logsI, 1, MaxPyramidSize);
        sampleScale = 0.5f + logs - logsI;
    }

    /// <summary>Returns destination-level and accumulated-pyramid weights, respectively.</summary>
    public static Vector2 CalculateMergeWeights(
        float intensity,
        float downIntensityOffset,
        float pyramidWeightsParam,
        int level,
        int levelCount)
    {
        var destinationLevelWeight = Mathf.Min(
            1f, Mathf.Pow(intensity * (level + 1f) / (levelCount - 1f), pyramidWeightsParam));
        var accumulatedPyramidWeight = Mathf.Min(
            1f, 1f + downIntensityOffset - destinationLevelWeight);
        return new Vector2(destinationLevelWeight, accumulatedPyramidWeight);
    }
}
