using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public enum VivifyRenderOrder
{
    BeforeSkybox, AfterSkybox, BeforeOpaque, AfterOpaque,
    BeforeAlpha, AfterAlpha, BeforeMainEffect, AfterMainEffect
}

/// <summary>Owns ordered blits and map render targets. Paused previews retain the last playback captures.</summary>
public sealed class VivifyRenderingManager : IDisposable
{
    private const string mainTarget = "_Main";
    private static readonly int mainCopy = Shader.PropertyToID("_VivifyMainCopy");
    private static readonly int blitCopy = Shader.PropertyToID("_VivifyBlitCopy");
    private static readonly CameraEvent[] cameraEvents =
    {
        CameraEvent.BeforeSkybox, CameraEvent.AfterSkybox,
        CameraEvent.BeforeForwardOpaque, CameraEvent.AfterForwardOpaque,
        CameraEvent.BeforeForwardAlpha, CameraEvent.AfterForwardAlpha
    };

    public VivifyCameraManager Cameras { get; }
    private readonly VivifyAssetBundleManager assets;
    private readonly ObjectPropertyManager properties;
    private readonly Dictionary<string, ScreenTexture> textures = new();
    private readonly Dictionary<string, ScreenTexture> retainedTextures = new();
    private readonly List<Blit> blits = new();
    private readonly CommandBuffer[] earlyPasses = new CommandBuffer[cameraEvents.Length];
    private Camera camera;
    private bool blitsEnabled;
    private float beat;
    private bool isPlaying;
    public Camera ActiveCamera => camera;

    public VivifyRenderingManager(TracksManager tracks, VivifyAssetBundleManager assets,
        ObjectPropertyManager properties)
    {
        this.assets = assets;
        this.properties = properties;
        Cameras = new VivifyCameraManager(tracks, properties.TextureBindings);
        for (var i = 0; i < earlyPasses.Length; i++)
            earlyPasses[i] = new CommandBuffer { name = "Vivify " + (VivifyRenderOrder)i };
        Camera.onPreCull += OnPreCull;
        Camera.onPreRender += OnPreRender;
        Camera.onPostRender += OnPostRender;
    }

    /// <summary>Moves camera captures to the selected camera and optionally attaches map blit passes.</summary>
    /// <param name="target">Selected camera, or null to detach rendering.</param>
    /// <param name="enableBlits">Whether this camera may run map blits.</param>
    public void AssignToCamera(Camera target, bool enableBlits = true)
    {
        if (camera == target && blitsEnabled == enableBlits) return;
        if (camera != null)
            for (var i = 0; i < earlyPasses.Length; i++) camera.RemoveCommandBuffer(cameraEvents[i], earlyPasses[i]);
        foreach (var command in earlyPasses) command.Clear();
        Cameras.SetMainCamera(target);
        camera = target;
        blitsEnabled = enableBlits;
        if (camera != null && blitsEnabled)
            for (var i = 0; i < earlyPasses.Length; i++) camera.AddCommandBuffer(cameraEvents[i], earlyPasses[i]);
    }

    public void CreateScreenTexture(JSONNode data)
    {
        var id = (string)data["id"];
        if (string.IsNullOrEmpty(id) || id == mainTarget || textures.ContainsKey(id))
        {
            Debug.LogWarning($"Vivify: invalid or duplicate screen texture id '{id}'.");
            return;
        }
        var xRatio = (data.HasKey("xRatio") ? data["xRatio"].AsFloat : 1f);
        var yRatio = (data.HasKey("yRatio") ? data["yRatio"].AsFloat : 1f);
        var width = data.HasKey("width") ? data["width"].AsInt : 0;
        var height = data.HasKey("height") ? data["height"].AsInt : 0;
        if (!(xRatio > 0) || float.IsInfinity(xRatio) || !(yRatio > 0) || float.IsInfinity(yRatio)
            || (data.HasKey("width") && width <= 0) || (data.HasKey("height") && height <= 0))
        {
            Debug.LogWarning($"Vivify: invalid dimensions for screen texture '{id}'.");
            return;
        }
        RenderTextureFormat? format = null;
        if (data.HasKey("colorFormat"))
        {
            if (!Enum.TryParse<RenderTextureFormat>(data["colorFormat"], out var parsed)
                || !Enum.IsDefined(typeof(RenderTextureFormat), parsed) || !SystemInfo.SupportsRenderTextureFormat(parsed))
            {
                Debug.LogWarning($"Vivify: unsupported color format for screen texture '{id}'.");
                return;
            }
            format = parsed;
        }
        var filter = FilterMode.Bilinear;
        if (data.HasKey("filterMode") && (!Enum.TryParse(data["filterMode"], out filter)
            || !Enum.IsDefined(typeof(FilterMode), filter)))
        {
            Debug.LogWarning($"Vivify: invalid filter mode for screen texture '{id}'.");
            return;
        }
        if (retainedTextures.Remove(id, out var retained))
        {
            if (retained.Matches(width, height, xRatio, yRatio, format, filter))
            {
                textures.Add(id, retained);
                return;
            }
            retained.Dispose();
        }
        textures.Add(id, new ScreenTexture(Cameras.TextureBindings, id, width, height, xRatio, yRatio, format, filter));
    }

    public void AddBlit(CustomEventStateData state)
    {
        var data = state.Base.Data;
        Material material = null;
        if (data["asset"].IsString && !assets.AssetPathToMaterial.TryGetValue(data["asset"], out material))
        {
            Debug.LogWarning($"Vivify: missing blit material '{data["asset"]}'.");
            return;
        }
        var order = VivifyRenderOrder.AfterMainEffect;
        if (data.HasKey("order") && (!Enum.TryParse(data["order"], out order)
            || !Enum.IsDefined(typeof(VivifyRenderOrder), order)))
        {
            Debug.LogWarning($"Vivify: invalid blit order '{data["order"]}'.");
            return;
        }
        var pass = (data.HasKey("pass") ? data["pass"].AsInt : -1);
        if (pass < -1 || (material != null && pass >= material.passCount))
        {
            Debug.LogWarning($"Vivify: invalid blit pass {pass}.");
            return;
        }
        var destinations = new List<string>();
        var destination = data["destination"];
        if (destination.IsString) destinations.Add(destination);
        else if (destination.IsArray)
            foreach (var item in destination.Children)
                if (item.IsString) destinations.Add(item);
        if (destinations.Count == 0) destinations.Add(mainTarget);

        if (material != null && data["properties"].IsArray) properties.AssignMaterial(state);
        var duration = state.Base.DataDuration ?? 0;
        if (duration < 0 || (duration > 0 && state.EndTimeAnimation <= beat)) return;
        var blit = new Blit(state, material, data["source"].IsString ? (string)data["source"] : mainTarget,
            destinations.ToArray(), pass, order, data.HasKey("priority") ? data["priority"].AsInt : 0);
        // A paused seek must not schedule snapshots from crossed event boundaries for the next playback frame.
        blit.Consumed = !isPlaying && blit.OneFrame && state.StartTime < beat;
        if (blit.Consumed) return;
        blits.Add(blit);
        // Upstream executes higher priorities first, then later registrations at equal priority.
        blits.Sort((a, b) => a.Priority != b.Priority
            ? b.Priority.CompareTo(a.Priority) : b.State.Sequence.CompareTo(a.State.Sequence));
    }

    public bool Destroy(string id)
    {
        if (Cameras.Destroy(id)) return true;
        if (!textures.Remove(id, out var texture)) return false;
        if (isPlaying) texture.Dispose();
        else Retain(id, texture);
        return true;
    }

    /// <summary>Samples the cursor. Persistent blit destinations update during playback only.</summary>
    /// <param name="songBpmBeat">Cursor position in beats at the song's base BPM.</param>
    /// <param name="playing">Whether playback may update persistent blit destinations.</param>
    public void Sample(float songBpmBeat, bool playing)
    {
        if (!isPlaying && playing)
            foreach (var blit in blits)
                if (blit.OneFrame && blit.State.StartTime < songBpmBeat) blit.Consumed = true;
        beat = songBpmBeat;
        isPlaying = playing;
        for (var i = blits.Count - 1; i >= 0; i--)
            if (blits[i].Consumed || (!blits[i].OneFrame && songBpmBeat >= blits[i].State.EndTimeAnimation))
                blits.RemoveAt(i);
    }

    public bool HasBlits(VivifyRenderOrder order)
    {
        foreach (var blit in blits)
            if (blit.Order == order && blit.IsActive(beat) && (isPlaying || blit.WritesMain)) return true;
        return false;
    }

    /// <summary>Records a complete ordered stage, including safe self-blits. Source and destination may be equal.</summary>
    public void RecordBlits(CommandBuffer command, VivifyRenderOrder order,
        RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTextureDescriptor descriptor)
    {
        command.GetTemporaryRT(mainCopy, descriptor, FilterMode.Bilinear);
        command.Blit(source, mainCopy);
        foreach (var blit in blits)
        {
            if (blit.Order != order || !blit.IsActive(beat)) continue;
            RenderTexture sourceTexture = null;
            if (blit.Source != mainTarget && !TryGetTexture(blit.Source, out sourceTexture))
            {
                blit.WarnMissing(blit.Source);
                continue;
            }
            foreach (var id in blit.Destinations)
            {
                if (!isPlaying && id != mainTarget) continue;
                RenderTexture destinationTexture = null;
                if (id != mainTarget && !TryGetScreenTexture(id, out destinationTexture))
                {
                    blit.WarnMissing(id);
                    continue;
                }
                var src = blit.Source == mainTarget ? new RenderTargetIdentifier(mainCopy) : new RenderTargetIdentifier(sourceTexture);
                var dst = id == mainTarget ? new RenderTargetIdentifier(mainCopy) : new RenderTargetIdentifier(destinationTexture);
                if (src.Equals(dst))
                {
                    if (blit.Material == null) continue;
                    var copyDescriptor = destinationTexture != null ? destinationTexture.descriptor : descriptor;
                    command.GetTemporaryRT(blitCopy, copyDescriptor, destinationTexture != null ? destinationTexture.filterMode : FilterMode.Bilinear);
                    command.Blit(src, blitCopy, blit.Material, blit.Pass);
                    command.Blit(blitCopy, dst);
                    command.ReleaseTemporaryRT(blitCopy);
                }
                else if (blit.Material != null) command.Blit(src, dst, blit.Material, blit.Pass);
                else command.Blit(src, dst);
                blit.Recorded = true;
            }
        }
        command.Blit(mainCopy, destination);
        command.ReleaseTemporaryRT(mainCopy);
    }

    public bool TryGetTexture(string id, out RenderTexture texture) =>
        TryGetScreenTexture(id, out texture) || Cameras.TryGetTexture(id, out texture);

    private bool TryGetScreenTexture(string id, out RenderTexture texture)
    {
        texture = null;
        if (!textures.TryGetValue(id, out var resource)) return false;
        texture = resource.Texture;
        return texture != null;
    }

    private void OnPreCull(Camera renderingCamera)
    {
        if (renderingCamera != camera) return;
        foreach (var blit in blits) blit.Recorded = false;
        var descriptor = GetDescriptor(camera);
        foreach (var texture in textures.Values) texture.Prepare(descriptor, isPlaying);
        Cameras.RenderCameras();
        Cameras.BeginCull(camera);
    }

    private void OnPreRender(Camera renderingCamera)
    {
        if (renderingCamera != camera || !blitsEnabled) return;
        var descriptor = GetDescriptor(camera);
        var target = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
        for (var i = 0; i < earlyPasses.Length; i++)
        {
            earlyPasses[i].Clear();
            if (HasBlits((VivifyRenderOrder)i)) RecordBlits(earlyPasses[i], (VivifyRenderOrder)i, target, target, descriptor);
        }
    }

    private void OnPostRender(Camera renderingCamera)
    {
        if (renderingCamera != camera) return;
        Cameras.EndCull(camera);
        for (var i = blits.Count - 1; i >= 0; i--)
        {
            var blit = blits[i];
            if (blit.Recorded && blit.OneFrame && (isPlaying || !blit.WritesTexture)) blits.RemoveAt(i);
        }
        foreach (var command in earlyPasses) command.Clear();
    }

    private void Retain(string id, ScreenTexture texture)
    {
        texture.Unbind();
        if (retainedTextures.Remove(id, out var previous)) previous.Dispose();
        retainedTextures.Add(id, texture);
    }

    /// <summary>Resets event state, optionally retaining screen and camera captures for same-map scrubbing.</summary>
    /// <param name="preserveBlitTextures">Keep captured textures when reconstructing the current map.</param>
    /// <param name="authoredTextureIds">Optional map declarations used to release captures whose declarations were deleted.</param>
    /// <param name="authoredCameraIds">Optional camera declarations used to release captures deleted from the map.</param>
    public void Reset(bool preserveBlitTextures = false, ISet<string> authoredTextureIds = null,
        ISet<string> authoredCameraIds = null)
    {
        Cameras.Reset(preserveBlitTextures, authoredCameraIds);
        foreach (var entry in textures)
        {
            if (preserveBlitTextures) Retain(entry.Key, entry.Value);
            else entry.Value.Dispose();
        }
        textures.Clear();
        if (!preserveBlitTextures)
        {
            foreach (var texture in retainedTextures.Values) texture.Dispose();
            retainedTextures.Clear();
        }
        else if (authoredTextureIds != null)
        {
            var removed = new List<string>();
            foreach (var entry in retainedTextures)
            {
                if (authoredTextureIds.Contains(entry.Key)) continue;
                entry.Value.Dispose();
                removed.Add(entry.Key);
            }
            foreach (var id in removed) retainedTextures.Remove(id);
        }
        if (!preserveBlitTextures) Cameras.TextureBindings.Reset();
        blits.Clear();
        foreach (var command in earlyPasses) command.Clear();
    }

    public void Dispose()
    {
        Reset();
        AssignToCamera(null);
        Camera.onPreCull -= OnPreCull;
        Camera.onPreRender -= OnPreRender;
        Camera.onPostRender -= OnPostRender;
        Cameras.Dispose();
        foreach (var command in earlyPasses) command.Release();
    }

    public static RenderTextureDescriptor GetDescriptor(Camera camera)
    {
        var descriptor = camera.targetTexture != null ? camera.targetTexture.descriptor
            : new RenderTextureDescriptor(Mathf.Max(camera.scaledPixelWidth, 1), Mathf.Max(camera.scaledPixelHeight, 1),
                camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 0);
        descriptor.depthBufferBits = 0;
        descriptor.depthStencilFormat = GraphicsFormat.None;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.useDynamicScale = false;
        descriptor.useDynamicScaleExplicit = false;
        return descriptor;
    }

    private sealed class Blit
    {
        public readonly CustomEventStateData State;
        public readonly Material Material;
        public readonly string Source;
        public readonly string[] Destinations;
        public readonly int Pass, Priority;
        public readonly VivifyRenderOrder Order;
        public readonly bool OneFrame;
        public readonly bool WritesMain;
        public readonly bool WritesTexture;
        public bool Recorded, Consumed;
        private readonly HashSet<string> warnedIds = new();

        public Blit(CustomEventStateData state, Material material, string source, string[] destinations,
            int pass, VivifyRenderOrder order, int priority)
        {
            State = state; Material = material; Source = source; Destinations = destinations;
            Pass = pass; Order = order; Priority = priority;
            OneFrame = (state.Base.DataDuration ?? 0) == 0;
            WritesMain = Array.IndexOf(destinations, mainTarget) >= 0;
            foreach (var destination in destinations)
                if (destination != mainTarget) WritesTexture = true;
        }
        public bool IsActive(float time) => !Consumed && time >= State.StartTime
            && (OneFrame || time < State.EndTimeAnimation);
        public void WarnMissing(string id)
        {
            if (warnedIds.Add(id)) Debug.LogWarning($"Vivify: blit texture '{id}' is unavailable.");
        }
    }

    private sealed class ScreenTexture : IDisposable
    {
        private readonly string name;
        private readonly int width, height;
        private readonly float xRatio, yRatio;
        private readonly RenderTextureFormat? format;
        private readonly FilterMode filter;
        private readonly VivifyTextureBindings bindings;
        private RenderTextureDescriptor preparedDescriptor;
        public RenderTexture Texture { get; private set; }

        public ScreenTexture(VivifyTextureBindings bindings, string name, int width, int height, float xRatio, float yRatio,
            RenderTextureFormat? format, FilterMode filter)
        {
            this.name = name; this.width = width; this.height = height;
            this.xRatio = xRatio; this.yRatio = yRatio; this.format = format; this.filter = filter;
            this.bindings = bindings;
        }
        public bool Matches(int width, int height, float xRatio, float yRatio,
            RenderTextureFormat? format, FilterMode filter) => this.width == width && this.height == height
            && this.xRatio == xRatio && this.yRatio == yRatio && this.format == format && this.filter == filter;

        public void Unbind() => bindings.Remove(this, name);

        public void Prepare(RenderTextureDescriptor descriptor, bool playing)
        {
            // Keep the last capture intact across paused viewport changes; playback adopts the new dimensions.
            if (Texture != null && !playing)
            {
                bindings.Set(this, name, Texture);
                return;
            }
            descriptor.width = Mathf.Clamp((int)((width > 0 ? width : descriptor.width) / xRatio), 1, SystemInfo.maxTextureSize);
            descriptor.height = Mathf.Clamp((int)((height > 0 ? height : descriptor.height) / yRatio), 1, SystemInfo.maxTextureSize);
            if (format.HasValue) descriptor.colorFormat = format.Value;
            // Unity adds creation flags to Texture.descriptor; compare the requested descriptors instead.
            if (Texture == null || !preparedDescriptor.Equals(descriptor))
            {
                bindings.Remove(this, name);
                Release();
                Texture = new RenderTexture(descriptor) { name = "Vivify " + name, filterMode = filter, hideFlags = HideFlags.HideAndDontSave };
                Texture.Create();
                preparedDescriptor = descriptor;
                var active = RenderTexture.active;
                RenderTexture.active = Texture;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = active;
            }
            bindings.Set(this, name, Texture);
        }
        private void Release()
        {
            if (Texture == null) return;
            Texture.Release();
            UnityEngine.Object.Destroy(Texture);
            Texture = null;
        }
        public void Dispose()
        {
            bindings.Remove(this, name);
            Release();
        }
    }
}
