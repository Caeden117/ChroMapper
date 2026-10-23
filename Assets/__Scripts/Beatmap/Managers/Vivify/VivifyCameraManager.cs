using System;
using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Owns map-created camera captures and reversible camera properties.</summary>
public sealed class VivifyCameraManager : IDisposable
{
    private const string mainId = "_Main";
    // Layer 16 is unused by ChroMapper's TagManager and authored assets. Layer 22 is the editor Event layer.
    private const int cullingLayer = 16;
    private static readonly int captureDepthId = Shader.PropertyToID("_VivifyCaptureDepthTexture");

    private readonly TracksManager tracks;
    private readonly Dictionary<string, CameraProperties> properties = new();
    private readonly Dictionary<string, CameraDeclaration> declarations = new();
    private readonly Dictionary<string, CameraDeclaration> retainedCaptures = new();
    private readonly Dictionary<Camera, CameraState> cameraStates = new();
    private readonly Dictionary<Camera, CullState> activeCulls = new();
    private readonly List<Renderer> childRenderers = new();
    private readonly List<Camera> culledCameras = new();

    private Camera mainCamera;
    private Material depthMaterial;
    private bool renderingCaptures;

    /// <summary>Renders ChroMapper's main image effect into a capture's color output.</summary>
    public Action<RenderTexture, RenderTexture> RenderMainEffect;
    public Action<Camera> BeforeCaptureRender;
    public Action<Camera> AfterCaptureRender;
    public VivifyTextureBindings TextureBindings { get; }

    public VivifyCameraManager(TracksManager tracks, VivifyTextureBindings textureBindings = null)
    {
        this.tracks = tracks;
        TextureBindings = textureBindings ?? new VivifyTextureBindings();
    }

    /// <summary>Restores the previous camera and applies the map's main-camera properties to the new camera.</summary>
    public void SetMainCamera(Camera camera)
    {
        if (mainCamera == camera) return;
        if (mainCamera != null)
        {
            EndCull(mainCamera);
            if (cameraStates.TryGetValue(mainCamera, out var oldState)) oldState.Restore(mainCamera);
            cameraStates.Remove(mainCamera);
        }
        foreach (var declaration in declarations.Values)
        {
            if (declaration.Capture == null) continue;
            EndCull(declaration.Capture.Camera);
            declaration.Capture.gameObject.SetActive(false);
        }

        mainCamera = camera;
        if (mainCamera == null) return;
        cameraStates[mainCamera] = new CameraState(mainId, mainCamera);
        ApplyProperties(mainCamera);
        foreach (var declaration in declarations.Values)
        {
            EnsureCapture(declaration);
            cameraStates[declaration.Capture.Camera] = new CameraState(declaration.Id, cameraStates[mainCamera]);
            declaration.Capture.gameObject.SetActive(true);
            ApplyProperties(declaration.Capture.Camera);
        }
    }

    /// <summary>Creates a capture following the main camera. Camera ids must be unique and cannot be _Main.</summary>
    public void CreateCamera(JSONNode data)
    {
        CameraDeclaration declaration;
        try
        {
            if (data == null || !data.IsObject) throw new ArgumentException("CreateCamera data must be an object.");
            var id = ReadId(data, false);
            if (id == mainId) throw new ArgumentException("CreateCamera cannot use the reserved _Main id.");
            if (declarations.ContainsKey(id)) throw new ArgumentException($"Camera [{id}] already exists.");
            declaration = new CameraDeclaration(id, ReadTextureName(data, "texture"), ReadTextureName(data, "depthTexture"));
            if (data.HasKey("properties")) UpdateProperties(id, data["properties"]);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Vivify CreateCamera: {exception.Message}");
            return;
        }
        if (retainedCaptures.Remove(declaration.Id, out var retained))
        {
            if (retained.TextureName == declaration.TextureName && retained.DepthTextureName == declaration.DepthTextureName)
            {
                declaration.Capture = retained.Capture;
                if (declaration.Capture != null && mainCamera != null)
                {
                    cameraStates[declaration.Capture.Camera] = new CameraState(declaration.Id, cameraStates[mainCamera]);
                    declaration.Capture.gameObject.SetActive(true);
                    ApplyProperties(declaration.Capture.Camera);
                }
            }
            else
                ReleaseCapture(retained);
        }
        declarations.Add(declaration.Id, declaration);
        EnsureCapture(declaration);
    }

    /// <summary>Updates supplied properties only. A null property restores the camera's captured default.</summary>
    public void SetCameraProperty(JSONNode data)
    {
        string id;
        try
        {
            if (data == null || !data.IsObject) throw new ArgumentException("SetCameraProperty data must be an object.");
            id = ReadId(data, true);
            UpdateProperties(id, data["properties"]);
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"Vivify SetCameraProperty: {exception.Message}");
            return;
        }
        if (id == mainId)
        {
            if (mainCamera != null) ApplyProperties(mainCamera);
        }
        else if (declarations.TryGetValue(id, out var declaration) && declaration.Capture != null)
        {
            ApplyProperties(declaration.Capture.Camera);
        }
    }

    /// <summary>Stops a camera, retaining its last color/depth outputs and properties until recreation or reset.</summary>
    public bool Destroy(string id)
    {
        if (!declarations.Remove(id, out var declaration)) return false;
        RetainCapture(declaration);
        return true;
    }

    public bool IsCaptureCamera(Camera camera) =>
        camera != null && cameraStates.TryGetValue(camera, out var state) && state.Id != mainId;

    public bool MainEffectEnabled(Camera camera) =>
        GetProperties(camera)?.MainEffect ?? true;

    public bool BloomPrePassEnabled(Camera camera) =>
        GetProperties(camera)?.BloomPrePass ?? true;

    public bool HasClearFlagsOverride(Camera camera) => GetProperties(camera)?.ClearFlags != null;

    /// <summary>Tests track culling for manual renderer paths that do not consult Camera.cullingMask.</summary>
    public bool IsVisible(Camera camera, GameObject target)
    {
        var culling = GetProperties(camera)?.Culling;
        if (culling == null || target == null) return true;
        if (!activeCulls.ContainsKey(camera)) CollectRoots(culling);
        var selected = false;
        foreach (var root in culling.Roots)
        {
            if (root == null || !target.transform.IsChildOf(root)) continue;
            selected = true;
            break;
        }
        return culling.Whitelist ? selected : !selected;
    }

    /// <summary>Renders captures before the main camera's culling changes any object layers.</summary>
    public void RenderCameras()
    {
        if (mainCamera == null || renderingCaptures || declarations.Count == 0) return;
        renderingCaptures = true;
        try
        {
            foreach (var declaration in declarations.Values)
            {
                EnsureCapture(declaration);
                var capture = declaration.Capture;
                if (capture == null) continue;
                capture.Prepare(mainCamera);
                ApplyProperties(capture.Camera);
                try
                {
                    BeforeCaptureRender?.Invoke(capture.Camera);
                    capture.Camera.Render();
                }
                finally
                {
                    EndCull(capture.Camera);
                    AfterCaptureRender?.Invoke(capture.Camera);
                }
            }
        }
        finally
        {
            renderingCaptures = false;
        }
    }

    /// <summary>Finds a live or stopped camera output by camera id or its declared global texture name.</summary>
    public bool TryGetTexture(string id, out RenderTexture texture)
    {
        if ((declarations.TryGetValue(id, out var declaration) || retainedCaptures.TryGetValue(id, out declaration))
            && declaration.Capture != null)
        {
            texture = declaration.Capture.ColorTexture;
            return texture != null;
        }
        return TryGetNamedTexture(declarations, id, out texture)
            || TryGetNamedTexture(retainedCaptures, id, out texture);
    }

    private static bool TryGetNamedTexture(Dictionary<string, CameraDeclaration> declarations, string id, out RenderTexture texture)
    {
        foreach (var entry in declarations.Values)
        {
            if (entry.Capture == null) continue;
            if (entry.TextureName == id) texture = entry.Capture.ColorTexture;
            else if (entry.DepthTextureName == id) texture = entry.Capture.DepthTexture;
            else continue;
            return texture != null;
        }
        texture = null;
        return false;
    }

    public void BeginCull(Camera camera)
    {
        var culling = GetProperties(camera)?.Culling;
        if (culling == null || activeCulls.ContainsKey(camera)) return;
        var state = cameraStates[camera].Cull;
        state.Mask = camera.cullingMask;
        activeCulls.Add(camera, state);
        camera.cullingMask = culling.Whitelist ? 1 << cullingLayer : state.Mask & ~(1 << cullingLayer);
        CollectRoots(culling);
        foreach (var root in culling.Roots)
        {
            if (root == null) continue;
            root.GetComponentsInChildren(true, childRenderers);
            foreach (var renderer in childRenderers)
            {
                if (renderer == null) continue;
                // Displacement objects are already hidden from native camera rendering and drawn manually.
                if (renderer.TryGetComponent<ScreenDisplacementRenderer>(out var displacement) && displacement.IsReady)
                    continue;
                var target = renderer.gameObject;
                if (!state.Layers.TryAdd(target, target.layer)) continue;
                target.layer = cullingLayer;
            }
        }
        childRenderers.Clear();
    }

    public void EndCull(Camera camera)
    {
        if (ReferenceEquals(camera, null) || !activeCulls.Remove(camera, out var state)) return;
        if (camera != null) camera.cullingMask = state.Mask;
        foreach (var entry in state.Layers)
            if (entry.Key != null) entry.Key.layer = entry.Value;
        state.Layers.Clear();
    }

    /// <summary>Restores camera defaults and event state, optionally retaining captures across same-map scrubbing.</summary>
    /// <param name="preserveTextures">Keep the last captured color/depth textures and their shader bindings.</param>
    /// <param name="authoredCameraIds">Optional declarations used to release captures deleted from the map.</param>
    public void Reset(bool preserveTextures = false, ISet<string> authoredCameraIds = null)
    {
        culledCameras.Clear();
        culledCameras.AddRange(activeCulls.Keys);
        foreach (var camera in culledCameras) EndCull(camera);
        culledCameras.Clear();
        foreach (var declaration in declarations.Values)
        {
            if (preserveTextures) RetainCapture(declaration);
            else ReleaseCapture(declaration);
        }
        declarations.Clear();
        var removed = new List<string>();
        foreach (var entry in retainedCaptures)
        {
            if (preserveTextures && (authoredCameraIds == null || authoredCameraIds.Contains(entry.Key))) continue;
            ReleaseCapture(entry.Value);
            removed.Add(entry.Key);
        }
        foreach (var id in removed) retainedCaptures.Remove(id);
        properties.Clear();
        if (mainCamera != null && cameraStates.TryGetValue(mainCamera, out var mainState)) mainState.Restore(mainCamera);
    }

    public void Dispose()
    {
        Reset();
        cameraStates.Clear();
        mainCamera = null;
        if (depthMaterial != null) Object.Destroy(depthMaterial);
        depthMaterial = null;
    }

    internal void RenderCapture(VivifyCameraCapture capture, RenderTexture source, RenderTexture destination)
    {
        if (MainEffectEnabled(capture.Camera) && RenderMainEffect != null)
            RenderMainEffect(source, destination);
        else
            Graphics.Blit(source, destination);
    }

    internal void RecordDepthCapture(CommandBuffer command, VivifyCameraCapture capture)
    {
        if (depthMaterial == null)
        {
            var shader = Resources.Load<Shader>("VivifyDepthCopy");
            if (shader == null) throw new InvalidOperationException("Vivify depth-copy shader is missing.");
            depthMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        // Copy the opaque render's depth so shaders without ShadowCaster passes still participate.
        // Doing this before transparent rendering preserves opaque-only camera depth semantics.
        command.SetGlobalTexture(captureDepthId, BuiltinRenderTextureType.CurrentActive, RenderTextureSubElement.Depth);
        command.Blit(BuiltinRenderTextureType.CameraTarget, capture.DepthTexture, depthMaterial);
        command.SetGlobalTexture(captureDepthId, Texture2D.blackTexture);
        command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
    }

    private void EnsureCapture(CameraDeclaration declaration)
    {
        if (mainCamera == null || declaration.Capture != null) return;
        var gameObject = new GameObject($"Vivify Camera [{declaration.Id}]") { hideFlags = HideFlags.DontSave };
        var camera = gameObject.AddComponent<Camera>();
        camera.CopyFrom(mainCamera);
        camera.enabled = false;
        camera.targetTexture = null;
        // Capture defaults come from the unmodified main camera, rather than a preceding map override.
        cameraStates[camera] = new CameraState(declaration.Id, cameraStates[mainCamera]);
        var capture = gameObject.AddComponent<VivifyCameraCapture>();
        capture.Initialize(this, camera, declaration.TextureName, declaration.DepthTextureName);
        declaration.Capture = capture;
        ApplyProperties(camera);
    }

    private void RetainCapture(CameraDeclaration declaration)
    {
        if (declaration.Capture != null)
        {
            EndCull(declaration.Capture.Camera);
            declaration.Capture.gameObject.SetActive(false);
        }
        retainedCaptures.Add(declaration.Id, declaration);
    }

    private void ReleaseCapture(CameraDeclaration declaration)
    {
        var capture = declaration.Capture;
        if (capture == null) return;
        EndCull(capture.Camera);
        cameraStates.Remove(capture.Camera);
        capture.Release();
        capture.gameObject.SetActive(false);
        Object.Destroy(capture.gameObject);
        declaration.Capture = null;
    }

    private static Transform GetCullingRoot(ObjectAnimator child)
    {
        // OEM environment controllers live on a separate GeometryContainer and animate LocalTarget directly.
        if (child.TargetType == ObjectAnimator.TargetTypes.Transform && child.LocalTarget != null)
            return child.LocalTarget;
        if (child.TargetType == ObjectAnimator.TargetTypes.Material)
        {
            var geometry = child.GetComponentInParent<GeometryContainer>();
            if (geometry != null) return geometry.transform;
        }
        return child.transform;
    }

    private static void CollectRoots(CullingProperties culling)
    {
        culling.Roots.Clear();
        culling.Visited.Clear();
        foreach (var track in culling.Tracks) CollectTrack(track, culling);
    }

    private static void CollectTrack(TrackAnimator track, CullingProperties culling)
    {
        if (track == null || !culling.Visited.Add(track)) return;
        foreach (var child in track.Children)
        {
            if (child == null) continue;
            culling.Roots.Add(GetCullingRoot(child));
            // AssignTrackParent can have logical children whose environment targets retain their OEM hierarchy.
            if (child.TryGetComponent<TrackAnimator>(out var childTrack)) CollectTrack(childTrack, culling);
        }
    }

    private CameraProperties GetProperties(Camera camera)
    {
        if (camera == null || !cameraStates.TryGetValue(camera, out var state)) return null;
        properties.TryGetValue(state.Id, out var value);
        return value;
    }

    private void ApplyProperties(Camera camera)
    {
        if (!cameraStates.TryGetValue(camera, out var state)) return;
        var value = GetProperties(camera);
        camera.depthTextureMode = state.DepthTextureMode | (value?.DepthTextureMode ?? DepthTextureMode.None);
        if (state.Id != mainId && declarations.TryGetValue(state.Id, out var declaration)
            && declaration.DepthTextureName != null)
            camera.depthTextureMode |= DepthTextureMode.Depth;
        camera.clearFlags = value?.ClearFlags ?? state.ClearFlags;
        camera.backgroundColor = value?.BackgroundColor ?? state.BackgroundColor;
    }

    private void UpdateProperties(string id, JSONNode data)
    {
        if (data == null || !data.IsObject) throw new ArgumentException("Camera properties must be an object.");
        properties.TryGetValue(id, out var previous);
        var value = previous == null ? new CameraProperties() : previous.Copy();
        if (data.HasKey("depthTextureMode"))
        {
            var node = data["depthTextureMode"];
            value.DepthTextureMode = null;
            if (node != null)
            {
                if (!node.IsArray) throw new ArgumentException("depthTextureMode must be an array or null.");
                var mode = DepthTextureMode.None;
                foreach (var entry in node.Children) mode |= ReadEnum<DepthTextureMode>(entry, "depthTextureMode");
                value.DepthTextureMode = mode;
            }
        }
        if (data.HasKey("clearFlags"))
            value.ClearFlags = data["clearFlags"] == null ? null : ReadEnum<CameraClearFlags>(data["clearFlags"], "clearFlags");
        if (data.HasKey("backgroundColor"))
        {
            var node = data["backgroundColor"];
            if (node != null && (!node.IsArray || node.Count < 3 || node.Count > 4))
                throw new ArgumentException("backgroundColor must contain three or four color channels, or null.");
            value.BackgroundColor = node == null ? null : node.ReadColor();
        }
        if (data.HasKey("culling")) value.Culling = ReadCulling(data["culling"]);
        if (data.HasKey("bloomPrePass")) value.BloomPrePass = ReadBool(data["bloomPrePass"], "bloomPrePass");
        if (data.HasKey("mainEffect")) value.MainEffect = ReadBool(data["mainEffect"], "mainEffect");
        properties[id] = value;
    }

    private CullingProperties ReadCulling(JSONNode data)
    {
        if (data == null) return null;
        if (!data.IsObject) throw new ArgumentException("culling must be an object or null.");
        var node = data["track"];
        var names = new List<string>();
        if (node != null && node.IsString) names.Add(node.Value);
        else if (node != null && node.IsArray)
            foreach (var entry in node.Children)
            {
                if (!entry.IsString) throw new ArgumentException("Culling tracks must be strings.");
                names.Add(entry.Value);
            }
        else throw new ArgumentException("Culling requires a track name or an array of track names.");
        var selectedTracks = new TrackAnimator[names.Count];
        for (var i = 0; i < names.Count; i++) selectedTracks[i] = tracks.GetAnimationTrack(names[i]);
        return new CullingProperties(selectedTracks, ReadBool(data["whitelist"], "whitelist") ?? false);
    }

    private static bool? ReadBool(JSONNode node, string field)
    {
        if (node == null) return null;
        if (!node.IsBoolean) throw new ArgumentException($"{field} must be a boolean or null.");
        return node.AsBool;
    }

    private static T ReadEnum<T>(JSONNode node, string field) where T : struct, Enum
    {
        if (node != null && node.IsString && Enum.TryParse<T>(node.Value, out var value) && Enum.IsDefined(typeof(T), value))
            return value;
        throw new ArgumentException($"Invalid {field} value [{node}].");
    }

    private static string ReadId(JSONNode data, bool allowDefault)
    {
        var node = data["id"];
        if (node == null && allowDefault) return mainId;
        if (node == null || !node.IsString || string.IsNullOrEmpty(node.Value))
            throw new ArgumentException("Camera id must be a nonempty string.");
        return node.Value;
    }

    private static string ReadTextureName(JSONNode data, string field)
    {
        var node = data[field];
        if (node == null) return null;
        if (!node.IsString || string.IsNullOrEmpty(node.Value)) throw new ArgumentException($"{field} must be a nonempty string.");
        return node.Value;
    }

    private sealed class CameraProperties
    {
        public DepthTextureMode? DepthTextureMode;
        public CameraClearFlags? ClearFlags;
        public Color? BackgroundColor;
        public CullingProperties Culling;
        public bool? BloomPrePass;
        public bool? MainEffect;
        public CameraProperties Copy() => (CameraProperties)MemberwiseClone();
    }

    private sealed class CullingProperties
    {
        public readonly TrackAnimator[] Tracks;
        public readonly bool Whitelist;
        public readonly List<Transform> Roots = new();
        public readonly HashSet<TrackAnimator> Visited = new();
        public CullingProperties(TrackAnimator[] tracks, bool whitelist)
        {
            Tracks = tracks;
            Whitelist = whitelist;
        }
    }

    private sealed class CameraDeclaration
    {
        public readonly string Id;
        public readonly string TextureName;
        public readonly string DepthTextureName;
        public VivifyCameraCapture Capture;
        public CameraDeclaration(string id, string textureName, string depthTextureName)
        {
            Id = id;
            TextureName = textureName;
            DepthTextureName = depthTextureName;
        }
    }

    private sealed class CameraState
    {
        public readonly string Id;
        public readonly DepthTextureMode DepthTextureMode;
        public readonly CameraClearFlags ClearFlags;
        public readonly Color BackgroundColor;
        public readonly CullState Cull = new();
        public CameraState(string id, Camera camera)
        {
            Id = id;
            DepthTextureMode = camera.depthTextureMode;
            ClearFlags = camera.clearFlags;
            BackgroundColor = camera.backgroundColor;
        }
        public CameraState(string id, CameraState state)
        {
            Id = id;
            DepthTextureMode = state.DepthTextureMode;
            ClearFlags = state.ClearFlags;
            BackgroundColor = state.BackgroundColor;
        }
        public void Restore(Camera camera)
        {
            camera.depthTextureMode = DepthTextureMode;
            camera.clearFlags = ClearFlags;
            camera.backgroundColor = BackgroundColor;
        }
    }

    private sealed class CullState
    {
        public int Mask;
        public readonly Dictionary<GameObject, int> Layers = new();
    }
}
