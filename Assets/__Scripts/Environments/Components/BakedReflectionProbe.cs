using UnityEngine;

[ExecuteAlways]
public class BakedReflectionProbe : MonoBehaviour
{
    private static readonly int reflectionProbeBoundsMinId = Shader.PropertyToID("_ReflectionProbeBoundsMin");
    private static readonly int reflectionProbeBoundsMaxId = Shader.PropertyToID("_ReflectionProbeBoundsMax");
    private static readonly int reflectionProbePositionId = Shader.PropertyToID("_ReflectionProbePosition");
    private static readonly int reflectionProbeTexture1Id = Shader.PropertyToID("_ReflectionProbeTexture1");
    private static readonly int reflectionProbeTexture2Id = Shader.PropertyToID("_ReflectionProbeTexture2");
    private static readonly int hasBakedReflectionProbeDataId = Shader.PropertyToID("_HasBakedReflectionProbeData");

    private static BakedReflectionProbe globalDataOwner;

    public int ResolutionBeforeDownsample = 2048;
    public int DownsampleByHalfCount = 1;
    public Vector3 Size;
    public Vector3 Offset;
    public ReflectionProbeDataSO ReflectionProbeData;

    private Cubemap blackCubemap;

    public Vector3 Position => transform.position;

    protected void OnEnable() => SendDataToShaders();

    // Component builders populate fields after AddComponent invokes OnEnable.
    protected void Start() => SendDataToShaders();

    protected void OnDisable()
    {
        // An older probe must not invalidate data published by a newer environment.
        if (globalDataOwner != this) return;

        Shader.SetGlobalFloat(hasBakedReflectionProbeDataId, 0f);
        globalDataOwner = null;
    }

    /// <summary>
    /// Publishes this probe's world-space bounds, position, and packed cubemaps to shader globals.
    /// Missing cubemaps use black textures; the availability flag is true only for a complete pair.
    /// </summary>
    /// <remarks>
    /// The last publisher owns the availability flag and clears it when disabled.
    /// Call on the Unity main thread after replacing the probe data or changing its bounds.
    /// </remarks>
    public void SendDataToShaders()
    {
        var position = transform.position;
        var boundsCenter = position + Offset;
        Shader.SetGlobalVector(reflectionProbeBoundsMinId, boundsCenter - Size * 0.5f);
        Shader.SetGlobalVector(reflectionProbeBoundsMaxId, boundsCenter + Size * 0.5f);
        Shader.SetGlobalVector(reflectionProbePositionId, position);
        Shader.SetGlobalTexture(
            reflectionProbeTexture1Id,
            ReflectionProbeData != null && ReflectionProbeData.ReflectionProbeCubemap1 != null
                ? ReflectionProbeData.ReflectionProbeCubemap1
                : GetOrCreateBlackCubemap());
        Shader.SetGlobalTexture(
            reflectionProbeTexture2Id,
            ReflectionProbeData != null && ReflectionProbeData.ReflectionProbeCubemap2 != null
                ? ReflectionProbeData.ReflectionProbeCubemap2
                : GetOrCreateBlackCubemap());
        var hasCompleteProbeData = ReflectionProbeData != null &&
            ReflectionProbeData.ReflectionProbeCubemap1 != null &&
            ReflectionProbeData.ReflectionProbeCubemap2 != null;
        Shader.SetGlobalFloat(hasBakedReflectionProbeDataId, hasCompleteProbeData ? 1f : 0f);
        globalDataOwner = this;
    }

    private Cubemap GetOrCreateBlackCubemap()
    {
        if (blackCubemap == null)
        {
            blackCubemap = new Cubemap(1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var black = new[] { Color.black };
            blackCubemap.SetPixels(black, CubemapFace.PositiveX);
            blackCubemap.SetPixels(black, CubemapFace.NegativeX);
            blackCubemap.SetPixels(black, CubemapFace.PositiveY);
            blackCubemap.SetPixels(black, CubemapFace.NegativeY);
            blackCubemap.SetPixels(black, CubemapFace.PositiveZ);
            blackCubemap.SetPixels(black, CubemapFace.NegativeZ);
            blackCubemap.Apply();
        }

        return blackCubemap;
    }
}
