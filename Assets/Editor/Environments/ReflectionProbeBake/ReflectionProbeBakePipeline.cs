using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor bake pipeline for BakedReflectionProbe components in generated
/// environment scenes. Renders the active environment scene once per
/// reflection bake ID (A-F) and cube face, applies the captured baking bloom
/// profile and the captured 0.2 bloom blend, packs the six bake IDs into two
/// cubemaps with the Reflection.hlsl channel layout (probe 1 RGB = bake IDs
/// A-C, probe 2 RGB = D-F), saves them as assets, and
/// assigns the resulting ReflectionProbeDataSO to every probe in the scene.
///
/// Usage: open a generated environment scene under Assets/__Scenes/Environments
/// and run "Environment/Bake Reflection Probes (Active Scene)".
/// </summary>
public static class ReflectionProbeBakePipeline
{
    private const string environmentsPath = "Assets/__Scenes/Environments";
    private const string reflectionProbesPath = "Assets/__Scenes/Environments/ReflectionProbes";
    private const string bloomShaderPath = "Assets/_Graphics/Shaders/Post Process/Bloom.shader";
    private const string packShaderPath =
        "Assets/Editor/Environments/ReflectionProbeBake/PackReflectionProbe.shader";
    private const string profileAssetPath =
        "Assets/Editor/Environments/ReflectionProbeBake/Reflection Probe Baking Bloom Profile.asset";

    // Values captured from the original bake configuration. The profile asset
    // carries radius/intensity/weights/down offset/threshold; these two values
    // have no slot in PyramidBloomProfileSO and live here.
    //
    // The original main-effect shader adds bloom at this intensity.
    private const float bakeBlend = 0.2f;

    private const int faceCount = 6;
    private const float bakeCameraNear = 0.1f;
    private const float bakeCameraFar = 1000f;

    // The six cube face camera orientations. This is the D3DCUBEMAP_FACES look
    // at/up table, the same table Unity's RenderToCubemap uses (see
    // SRP Core CoreUtils.lookAtList/upVectorList). Combined with the 2D
    // RenderTexture readback path (RenderTexture.active + ReadPixels +
    // Cubemap.SetPixels) the rendered face images land in the face layout the
    // cubemap sampler expects with no extra flips: the image top corresponds
    // to the face up vector and the image left to the left neighbour face in
    // Unity's vertical-cross cubemap layout.
    //
    // NOTE: not verified in a live editor; if baked reflections appear
    // mirrored, flip the X axis of the readback texture (or negate
    // projectionMatrix[0, 0]) and remove this comment.
    private static readonly Vector3[] faceDirections =
    {
        Vector3.right, Vector3.left,
        Vector3.up, Vector3.down,
        Vector3.forward, Vector3.back
    };

    private static readonly Vector3[] faceUps =
    {
        Vector3.up, Vector3.up,
        Vector3.back, Vector3.forward,
        Vector3.up, Vector3.up
    };

    // Bloom pyramid globals (mirrors BloomRenderer.RecordRender's usage of the
    // Bloom shader).
    private static readonly int combineParamsId = Shader.PropertyToID("_CombineParams");
    private static readonly int sampleScaleId = Shader.PropertyToID("_SampleScale");
    private static readonly int bloomTexId = Shader.PropertyToID("_BloomTex");
    private static readonly int bloomTexelSizeId = Shader.PropertyToID("_BloomTexelSize");
    private static readonly int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private static readonly int[] mipDownIds =
        BloomRenderUtility.CreateTextureIds("_ReflectionProbeBakeMipDown_");
    private static readonly int[] mipUpIds =
        BloomRenderUtility.CreateTextureIds("_ReflectionProbeBakeMipUp_");

    // Pack shader properties.
    private static readonly int probeRawTexId = Shader.PropertyToID("_ProbeRawTex");
    private static readonly int probeBloomTexId = Shader.PropertyToID("_ProbeBloomTex");
    private static readonly int probeSourceAId = Shader.PropertyToID("_ProbeSourceA");
    private static readonly int probeSourceBId = Shader.PropertyToID("_ProbeSourceB");
    private static readonly int probeSourceCId = Shader.PropertyToID("_ProbeSourceC");
    private static readonly int probeBlendId = Shader.PropertyToID("_ProbeBlend");
    private static readonly int probeSourceTexelOffsetId = Shader.PropertyToID("_ProbeSourceTexelOffset");

    public static void BakeSceneReflectionProbes(Scene scene)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) ||
            !scene.path.StartsWith(environmentsPath + "/", StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[ReflectionProbeBake] Active scene '{scene.name}' ('{scene.path}') is not a generated " +
                $"environment scene under {environmentsPath}. Open a generated environment scene first.");
            return;
        }

        var probes = scene.GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<BakedReflectionProbe>(true))
            .OrderBy(x => GetHierarchyPath(x.transform), StringComparer.Ordinal)
            .ToArray();
        if (probes.Length == 0)
        {
            Debug.LogError(
                $"[ReflectionProbeBake] Scene '{scene.name}' contains no BakedReflectionProbe components. " +
                "Regenerate the scene from its environment data first.");
            return;
        }
        if (probes.Length > 1)
            throw new InvalidOperationException(
                $"Scene '{scene.name}' contains {probes.Length} baked reflection probes. " +
                "The runtime shader supports one global probe per scene.");

        var bloomShader = AssetDatabase.LoadAssetAtPath<Shader>(bloomShaderPath);
        var packShader = AssetDatabase.LoadAssetAtPath<Shader>(packShaderPath);
        var profile = AssetDatabase.LoadAssetAtPath<PyramidBloomProfileSO>(profileAssetPath);
        if (bloomShader == null)
        {
            Debug.LogError($"[ReflectionProbeBake] Bloom shader was not found at '{bloomShaderPath}'.");
            return;
        }

        if (packShader == null)
        {
            Debug.LogError($"[ReflectionProbeBake] Pack shader was not found at '{packShaderPath}'.");
            return;
        }

        if (profile == null)
        {
            Debug.LogError($"[ReflectionProbeBake] Bake bloom profile was not found at '{profileAssetPath}'.");
            return;
        }

        if (!bloomShader.isSupported || !packShader.isSupported)
        {
            Debug.LogError("[ReflectionProbeBake] The bake shaders are not supported on this graphics device.");
            return;
        }

        foreach (var probe in probes)
        {
            if (probe.ResolutionBeforeDownsample < 8)
            {
                Debug.LogError(
                    $"[ReflectionProbeBake] Probe '{probe.name}' has ResolutionBeforeDownsample " +
                    $"{probe.ResolutionBeforeDownsample}; it must be at least 8.");
                return;
            }

            if (probe.DownsampleByHalfCount < 0)
            {
                Debug.LogError(
                    $"[ReflectionProbeBake] Probe '{probe.name}' has a negative DownsampleByHalfCount.");
                return;
            }
        }

        var bloomMaterial = new Material(bloomShader)
        {
            name = "ReflectionProbeBake Bloom",
            hideFlags = HideFlags.HideAndDontSave
        };
        bloomMaterial.SetFloat(bloomThresholdId, profile.BloomThreshold);

        var packMaterial = new Material(packShader)
        {
            name = "ReflectionProbeBake Pack",
            hideFlags = HideFlags.HideAndDontSave
        };
        packMaterial.SetFloat(probeBlendId, bakeBlend);

        var sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
        var bakeLighting = new BakeLightingState(scene);
        var globalState = CaptureGlobalShaderState();
        var bakeCamera = CreateBakeCamera();
        try
        {
            for (var probeIndex = 0; probeIndex < probes.Length; probeIndex++)
                BakeProbe(
                    probes[probeIndex],
                    probeIndex,
                    sceneName,
                    bakeCamera,
                    bakeLighting,
                    profile,
                    bloomMaterial,
                    packMaterial);
        }
        finally
        {
            DestroyBakeCamera(bakeCamera);
            bakeLighting.Restore();
            RestoreGlobalShaderState(globalState);
            UnityEngine.Object.DestroyImmediate(bloomMaterial);
            UnityEngine.Object.DestroyImmediate(packMaterial);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[ReflectionProbeBake] Baked {probes.Length} reflection probe(s) for scene '{sceneName}' " +
            $"into {reflectionProbesPath}/{sceneName}.");
    }
    private static void BakeProbe(
        BakedReflectionProbe probe,
        int probeIndex,
        string sceneName,
        Camera bakeCamera,
        BakeLightingState bakeLighting,
        PyramidBloomProfileSO profile,
        Material bloomMaterial,
        Material packMaterial)
    {
        // The probe's authored sizes drive the render sizes: the raw face
        // render matches ResolutionBeforeDownsample and the packed cubemap
        // halves it DownsampleByHalfCount times.
        var sourceWidth = Mathf.Max(8, probe.ResolutionBeforeDownsample);
        var cubemapSize = Mathf.Max(1, sourceWidth >> Mathf.Max(0, probe.DownsampleByHalfCount));

        var rawFace = CreateRenderTexture(
            sourceWidth, sourceWidth, RenderTextureFormat.ARGBHalf, "ReflectionProbeBake Raw Face");
        var bloomFace = CreateRenderTexture(
            sourceWidth,
            sourceWidth,
            BloomRenderUtility.GetBloomTextureFormat(),
            "ReflectionProbeBake Bloom Face");
        var baked = new RenderTexture[faceCount];
        var packedProbe1Face = CreateRenderTexture(
            cubemapSize, cubemapSize, RenderTextureFormat.ARGBHalf, "ReflectionProbeBake Packed Face 1");
        var packedProbe2Face = CreateRenderTexture(
            cubemapSize, cubemapSize, RenderTextureFormat.ARGBHalf, "ReflectionProbeBake Packed Face 2");

        var cubemap1 = new Cubemap(cubemapSize, TextureFormat.RGBAHalf, true)
        {
            name = $"{sceneName}_Probe{probeIndex}_Probe1"
        };
        var cubemap2 = new Cubemap(cubemapSize, TextureFormat.RGBAHalf, true)
        {
            name = $"{sceneName}_Probe{probeIndex}_Probe2"
        };

        try
        {
            ConfigureBakeCamera(bakeCamera, probe);

            // Non-zero offsets turn the blend pass taps into a 2x2 source
            // texel box that performs the downsample to the cubemap size.
            var downsample = cubemapSize < sourceWidth;
            packMaterial.SetVector(
                probeSourceTexelOffsetId,
                new Vector4(
                    downsample ? 0.5f / sourceWidth : 0f,
                    downsample ? 0.5f / sourceWidth : 0f,
                    0f,
                    0f));

            bakeCamera.targetTexture = rawFace;

            for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
            {
                bakeCamera.transform.SetPositionAndRotation(
                    probe.Position,
                    Quaternion.LookRotation(faceDirections[faceIndex], faceUps[faceIndex]));

                for (var bakeIdIndex = 0; bakeIdIndex < LightConstants.AllBakeIds.Count; bakeIdIndex++)
                {
                    // Isolate the bake ID: the environment shaders gate their
                    // lightmap channels by the global _LightmapLightBakeId
                    // colors (Lighting.hlsl), so rendering with only the
                    // active ID white and the rest black captures exactly that
                    // ID's static light contribution. The light probe globals
                    // are set too for contract completeness.
                    bakeLighting.Apply(LightConstants.AllBakeIds[bakeIdIndex]);
                    SetBakeIdGlobals(LightConstants.AllBakeIds[bakeIdIndex]);

                    bakeCamera.Render();
                    ApplyBloom(rawFace, bloomFace, profile, bloomMaterial);

                    if (baked[bakeIdIndex] == null)
                        baked[bakeIdIndex] = CreateRenderTexture(
                            cubemapSize,
                            cubemapSize,
                            RenderTextureFormat.ARGBHalf,
                            $"ReflectionProbeBake Baked {LightConstants.AllBakeIds[bakeIdIndex]}");

                    // Blend the raw render and the profile bloom and downsample
                    // to the packed cubemap size in one pass.
                    packMaterial.SetTexture(probeRawTexId, rawFace);
                    packMaterial.SetTexture(probeBloomTexId, bloomFace);
                    Graphics.Blit(rawFace, baked[bakeIdIndex], packMaterial, 0);
                }

                // Pack bake IDs A-C into probe 1 and D-F into probe 2
                // (Reflection.hlsl:DecodeReflectionProbePair contract).
                packMaterial.SetTexture(probeSourceAId, baked[0]);
                packMaterial.SetTexture(probeSourceBId, baked[1]);
                packMaterial.SetTexture(probeSourceCId, baked[2]);
                Graphics.Blit(baked[0], packedProbe1Face, packMaterial, 1);
                CopyPackedFaceToCubemap(
                    packedProbe1Face, cubemap1, (CubemapFace)faceIndex);

                packMaterial.SetTexture(probeSourceAId, baked[3]);
                packMaterial.SetTexture(probeSourceBId, baked[4]);
                packMaterial.SetTexture(probeSourceCId, baked[5]);
                Graphics.Blit(baked[3], packedProbe2Face, packMaterial, 1);
                CopyPackedFaceToCubemap(
                    packedProbe2Face, cubemap2, (CubemapFace)faceIndex);
            }

            SaveProbeAssets(sceneName, probeIndex, cubemap1, cubemap2, probe);
        }
        finally
        {
            bakeCamera.targetTexture = null;
            ReleaseRenderTexture(ref rawFace);
            ReleaseRenderTexture(ref bloomFace);
            for (var i = 0; i < baked.Length; i++) ReleaseRenderTexture(ref baked[i]);
            ReleaseRenderTexture(ref packedProbe1Face);
            ReleaseRenderTexture(ref packedProbe2Face);

            // After SaveProbeAssets the cubemaps belong to the AssetDatabase;
            // destroying them would delete the just-created assets.
            if (!EditorUtility.IsPersistent(cubemap1)) UnityEngine.Object.DestroyImmediate(cubemap1);
            if (!EditorUtility.IsPersistent(cubemap2)) UnityEngine.Object.DestroyImmediate(cubemap2);
        }
    }

    /// <summary>
    /// Runs the Bloom pyramid over a face render, mirroring
    /// BloomRenderer.RecordRender (same shader, passes, merge weights and
    /// per-level _CombineParams shaping) driven by the bake profile.
    /// </summary>
    private static void ApplyBloom(
        RenderTexture source,
        RenderTexture destination,
        PyramidBloomProfileSO profile,
        Material bloomMaterial)
    {
        var format = BloomRenderUtility.GetBloomTextureFormat();
        BloomRenderUtility.CalculatePyramidParameters(
            source.width, source.height, profile.Radius, out var iterations, out var sampleScale);
        var descriptors = BloomRenderUtility.BuildPyramidDescriptors(
            source.width, source.height, iterations, format);

        var commandBuffer = new CommandBuffer { name = "ReflectionProbeBake.Bloom" };
        try
        {
            commandBuffer.SetGlobalVector(
                combineParamsId, new Vector4(profile.Intensity, 1f, 0f, 0f));

            var lastDown = (RenderTargetIdentifier)source;
            var lastDownWidth = source.width;
            var lastDownHeight = source.height;
            for (var i = 0; i < iterations; i++)
            {
                commandBuffer.GetTemporaryRT(mipDownIds[i], descriptors[i], FilterMode.Bilinear);
                commandBuffer.SetGlobalVector(
                    bloomTexelSizeId, GetTexelSize(lastDownWidth, lastDownHeight));
                commandBuffer.Blit(lastDown, mipDownIds[i], bloomMaterial, i == 0 ? 0 : 1);
                lastDown = new RenderTargetIdentifier(mipDownIds[i]);
                lastDownWidth = descriptors[i].width;
                lastDownHeight = descriptors[i].height;
            }

            commandBuffer.SetGlobalFloat(sampleScaleId, sampleScale);
            var lastUp = new RenderTargetIdentifier(mipDownIds[iterations - 1]);
            var lastUpWidth = descriptors[iterations - 1].width;
            var lastUpHeight = descriptors[iterations - 1].height;
            for (var i = iterations - 2; i >= 1; i--)
            {
                commandBuffer.GetTemporaryRT(mipUpIds[i], descriptors[i], FilterMode.Bilinear);
                commandBuffer.SetGlobalVector(
                    bloomTexelSizeId, GetTexelSize(lastUpWidth, lastUpHeight));
                commandBuffer.SetGlobalTexture(bloomTexId, mipDownIds[i]);
                SetMergeParams(
                    commandBuffer,
                    i,
                    iterations,
                    profile,
                    i == iterations - 2 ? profile.FirstUpsampleBrightness : 1f);
                commandBuffer.Blit(lastUp, mipUpIds[i], bloomMaterial, 2);
                lastUp = new RenderTargetIdentifier(mipUpIds[i]);
                lastUpWidth = descriptors[i].width;
                lastUpHeight = descriptors[i].height;
            }

            commandBuffer.SetGlobalTexture(bloomTexId, mipDownIds[0]);
            commandBuffer.SetGlobalVector(
                bloomTexelSizeId, GetTexelSize(lastUpWidth, lastUpHeight));

            if (iterations == 1)
                commandBuffer.SetGlobalVector(combineParamsId, new Vector4(1f, 0f, 0f, 0f));
            else
                SetMergeParams(commandBuffer, 0, iterations, profile, profile.FinalUpsampleBrightness);

            commandBuffer.Blit(lastUp, destination, bloomMaterial, 2);

            commandBuffer.SetGlobalTexture(bloomTexId, Texture2D.blackTexture);
            for (var i = 0; i < iterations; i++) commandBuffer.ReleaseTemporaryRT(mipDownIds[i]);
            for (var i = 1; i < iterations - 1; i++) commandBuffer.ReleaseTemporaryRT(mipUpIds[i]);

            Graphics.ExecuteCommandBuffer(commandBuffer);
        }
        finally
        {
            commandBuffer.Dispose();
        }
    }

    private static void SetMergeParams(
        CommandBuffer commandBuffer,
        int level,
        int iterations,
        PyramidBloomProfileSO profile,
        float brightness)
    {
        var mergeWeights = BloomRenderUtility.CalculateMergeWeights(
            profile.Intensity,
            profile.DownIntensityOffset,
            profile.PyramidWeightsParam,
            level,
            iterations);
        commandBuffer.SetGlobalVector(
            combineParamsId,
            new Vector4(
                mergeWeights.x * brightness,
                mergeWeights.y * brightness,
                0f,
                0f));
    }

    private static void SetBakeIdGlobals(LightConstants.BakeId activeBakeId)
    {
        foreach (var bakeId in LightConstants.AllBakeIds)
        {
            var color = bakeId == activeBakeId ? Color.white : Color.black;
            Shader.SetGlobalColor(LightConstants.GetLightmapLightBakeIdPropertyId(bakeId), color);
            Shader.SetGlobalColor(LightConstants.GetLightProbeLightBakeIdPropertyId(bakeId), color);
        }
    }

    private static void ConfigureBakeCamera(Camera bakeCamera, BakedReflectionProbe probe)
    {
        bakeCamera.transform.SetPositionAndRotation(probe.Position, Quaternion.identity);
        bakeCamera.fieldOfView = 90f;
        bakeCamera.nearClipPlane = bakeCameraNear;
        bakeCamera.farClipPlane = bakeCameraFar;
        // Explicit square 90-degree projection; the square target texture
        // would imply the same aspect, but the matrix documents the intent
        // (see the face orientation note at the top of this file).
        bakeCamera.projectionMatrix = Matrix4x4.Perspective(90f, 1f, bakeCameraNear, bakeCameraFar);
    }

    private static Camera CreateBakeCamera()
    {
        var gameObject = new GameObject("ReflectionProbeBakeCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var camera = gameObject.AddComponent<Camera>();
        // Disabled cameras still render through Camera.Render(), the same
        // pattern Unity's RenderToCubemap baking examples use.
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = ~0;
        camera.allowHDR = true;
        camera.allowMSAA = false;
        camera.fieldOfView = 90f;
        camera.nearClipPlane = bakeCameraNear;
        camera.farClipPlane = bakeCameraFar;
        camera.depth = -100f;
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        return camera;
    }

    private static void DestroyBakeCamera(Camera camera)
    {
        if (camera == null) return;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }

    private static void CopyPackedFaceToCubemap(
        RenderTexture packedFace,
        Cubemap cubemap,
        CubemapFace face)
    {
        var previousActive = RenderTexture.active;
        var readback = new Texture2D(
            packedFace.width,
            packedFace.height,
            TextureFormat.RGBAHalf,
            false,
            true);
        try
        {
            RenderTexture.active = packedFace;
            readback.ReadPixels(new Rect(0f, 0f, packedFace.width, packedFace.height), 0, 0);
            readback.Apply(false, false);
            cubemap.SetPixels(readback.GetPixels(0), face, 0);
        }
        finally
        {
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(readback);
        }
    }

    private static void SaveProbeAssets(
        string sceneName,
        int probeIndex,
        Cubemap probe1,
        Cubemap probe2,
        BakedReflectionProbe probe)
    {
        var folderPath = $"{reflectionProbesPath}/{sceneName}";
        PrepareOutputFolder(sceneName);
        probe1.Apply(true, false);
        probe2.Apply(true, false);
        AssetDatabase.CreateAsset(probe1, $"{folderPath}/{probe1.name}.cubemap");
        AssetDatabase.CreateAsset(probe2, $"{folderPath}/{probe2.name}.cubemap");

        var data = ScriptableObject.CreateInstance<ReflectionProbeDataSO>();
        data.name = $"{sceneName}_Probe{probeIndex}_Data";
        data.ReflectionProbeCubemap1 = probe1;
        data.ReflectionProbeCubemap2 = probe2;
        AssetDatabase.CreateAsset(data, $"{folderPath}/{data.name}.asset");

        probe.ReflectionProbeData = data;
        EditorUtility.SetDirty(probe);
        probe.SendDataToShaders();
    }

    private static void PrepareOutputFolder(string sceneName)
    {
        var folderPath = $"{reflectionProbesPath}/{sceneName}";
        if (AssetDatabase.AssetPathExists(folderPath) && !AssetDatabase.DeleteAsset(folderPath))
            throw new InvalidOperationException($"Failed to replace '{folderPath}'.");

        if (!AssetDatabase.AssetPathExists(reflectionProbesPath)
            && AssetDatabase.CreateFolder(environmentsPath, "ReflectionProbes").Length == 0)
            throw new InvalidOperationException($"Failed to create '{reflectionProbesPath}'.");

        if (AssetDatabase.CreateFolder(reflectionProbesPath, sceneName).Length == 0)
            throw new InvalidOperationException($"Failed to create '{folderPath}'.");
    }

    private static RenderTexture CreateRenderTexture(
        int width,
        int height,
        RenderTextureFormat format,
        string name)
    {
        var renderTexture = new RenderTexture(
            width,
            height,
            0,
            format,
            RenderTextureReadWrite.Linear)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        renderTexture.Create();
        return renderTexture;
    }

    private static void ReleaseRenderTexture(ref RenderTexture renderTexture)
    {
        if (renderTexture == null) return;
        renderTexture.Release();
        UnityEngine.Object.DestroyImmediate(renderTexture);
        renderTexture = null;
    }

    private struct GlobalShaderState
    {
        public Color[] LightmapBakeIdColors;
        public Color[] ProbeBakeIdColors;
        public Vector4 CombineParams;
        public float SampleScale;
        public Vector4 BloomTexelSize;
        public Texture BloomTex;
        public Texture GlobalIntensityTex;
    }

    private static GlobalShaderState CaptureGlobalShaderState()
    {
        var bakeIdCount = LightConstants.AllBakeIds.Count;
        var lightmapColors = new Color[bakeIdCount];
        var probeColors = new Color[bakeIdCount];
        for (var i = 0; i < bakeIdCount; i++)
        {
            var bakeId = LightConstants.AllBakeIds[i];
            lightmapColors[i] =
                Shader.GetGlobalColor(LightConstants.GetLightmapLightBakeIdPropertyId(bakeId));
            probeColors[i] =
                Shader.GetGlobalColor(LightConstants.GetLightProbeLightBakeIdPropertyId(bakeId));
        }

        return new GlobalShaderState
        {
            LightmapBakeIdColors = lightmapColors,
            ProbeBakeIdColors = probeColors,
            CombineParams = Shader.GetGlobalVector(combineParamsId),
            SampleScale = Shader.GetGlobalFloat(sampleScaleId),
            BloomTexelSize = Shader.GetGlobalVector(bloomTexelSizeId),
            BloomTex = Shader.GetGlobalTexture(bloomTexId),
            GlobalIntensityTex = Shader.GetGlobalTexture("_GlobalIntensityTex")
        };
    }

    private static void RestoreGlobalShaderState(GlobalShaderState state)
    {
        for (var i = 0; i < LightConstants.AllBakeIds.Count; i++)
        {
            var bakeId = LightConstants.AllBakeIds[i];
            Shader.SetGlobalColor(
                LightConstants.GetLightmapLightBakeIdPropertyId(bakeId),
                state.LightmapBakeIdColors[i]);
            Shader.SetGlobalColor(
                LightConstants.GetLightProbeLightBakeIdPropertyId(bakeId),
                state.ProbeBakeIdColors[i]);
        }

        Shader.SetGlobalVector(combineParamsId, state.CombineParams);
        Shader.SetGlobalFloat(sampleScaleId, state.SampleScale);
        Shader.SetGlobalVector(bloomTexelSizeId, state.BloomTexelSize);
        Shader.SetGlobalTexture(bloomTexId, state.BloomTex);
        Shader.SetGlobalTexture("_GlobalIntensityTex", state.GlobalIntensityTex);
    }

    private static Vector4 GetTexelSize(int width, int height) =>
        new(1f / width, 1f / height, width, height);

    private sealed class BakeLightingState
    {
        private readonly LightController[] controllers;
        private readonly Color[] colors;
        private readonly LightmapLightsController[] lightmapControllers;
        private readonly LightmapsLightsController[] lightmapsControllers;
        private readonly Dictionary<(LightController.LightKind kind, int type, int id), HashSet<LightConstants.BakeId>>
            bakeIdsByLight = new();

        public BakeLightingState(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            controllers = roots
                .SelectMany(x => x.GetComponentsInChildren<LightController>(true))
                .Distinct()
                .ToArray();
            lightmapControllers = roots
                .SelectMany(x => x.GetComponentsInChildren<LightmapLightsController>(true))
                .Distinct()
                .ToArray();
            lightmapsControllers = roots
                .SelectMany(x => x.GetComponentsInChildren<LightmapsLightsController>(true))
                .Distinct()
                .ToArray();

            foreach (var controller in controllers) controller.Start();
            colors = controllers.Select(x => x.Color).ToArray();

            foreach (var controller in lightmapControllers)
            foreach (var data in controller.LightIntensityData)
                Add(data, controller.BakeId);

            foreach (var controller in lightmapsControllers)
            foreach (var data in controller.LightIntensityData)
                Add(data, data.BakeId);
        }

        public void Apply(LightConstants.BakeId bakeId)
        {
            foreach (var controller in controllers)
            {
                var key = (controller.Kind, controller.Type, controller.ID);
                var enabled = bakeIdsByLight.TryGetValue(key, out var bakeIds) && bakeIds.Contains(bakeId);
                controller.SetColor(enabled ? Color.white : Color.clear);
            }

            RefreshLightmaps();
        }

        public void Restore()
        {
            for (var i = 0; i < controllers.Length; i++) controllers[i].SetColor(colors[i]);
            RefreshLightmaps();
        }

        private void Add(LightController controller, LightConstants.BakeId bakeId)
        {
            var key = (controller.Kind, controller.Type, controller.ID);
            if (!bakeIdsByLight.TryGetValue(key, out var bakeIds))
            {
                bakeIds = new HashSet<LightConstants.BakeId>();
                bakeIdsByLight.Add(key, bakeIds);
            }

            bakeIds.Add(bakeId);
        }

        private void RefreshLightmaps()
        {
            foreach (var controller in lightmapControllers) controller.Refresh();
            foreach (var controller in lightmapsControllers) controller.Refresh();
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }
}
