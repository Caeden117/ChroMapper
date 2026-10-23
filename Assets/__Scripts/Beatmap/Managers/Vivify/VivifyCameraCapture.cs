using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>Renders one Vivify camera declaration without adding another automatic scene camera.</summary>
[RequireComponent(typeof(Camera))]
public sealed class VivifyCameraCapture : MonoBehaviour
{
    private VivifyCameraManager owner;
    private string colorName;
    private string depthName;
    private CommandBuffer depthCapture;

    public Camera Camera { get; private set; }
    public RenderTexture ColorTexture { get; private set; }
    public RenderTexture DepthTexture { get; private set; }

    internal void Initialize(VivifyCameraManager owner, Camera camera, string colorName, string depthName)
    {
        this.owner = owner;
        Camera = camera;
        this.colorName = colorName;
        this.depthName = depthName;
    }

    internal void Prepare(Camera mainCamera)
    {
        var mainTransform = mainCamera.transform;
        transform.SetPositionAndRotation(mainTransform.position, mainTransform.rotation);
        Camera.fieldOfView = mainCamera.fieldOfView;
        Camera.aspect = mainCamera.aspect;
        Camera.orthographic = mainCamera.orthographic;
        Camera.orthographicSize = mainCamera.orthographicSize;
        Camera.nearClipPlane = mainCamera.nearClipPlane;
        Camera.farClipPlane = mainCamera.farClipPlane;
        Camera.allowHDR = mainCamera.allowHDR;
        Camera.allowMSAA = false;
        Camera.renderingPath = mainCamera.renderingPath;
        Camera.useOcclusionCulling = mainCamera.useOcclusionCulling;
        Camera.cullingMask = mainCamera.cullingMask;
        Camera.layerCullDistances = mainCamera.layerCullDistances;
        Camera.layerCullSpherical = mainCamera.layerCullSpherical;
        Camera.projectionMatrix = mainCamera.projectionMatrix;
        Camera.nonJitteredProjectionMatrix = mainCamera.nonJitteredProjectionMatrix;
        Camera.worldToCameraMatrix = mainCamera.worldToCameraMatrix;
        Camera.cullingMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
        // Captures cover the main viewport rather than applying its screen-space rect a second time.
        Camera.rect = new Rect(0, 0, 1, 1);

        var descriptor = mainCamera.targetTexture != null
            ? mainCamera.targetTexture.descriptor
            : new RenderTextureDescriptor(
                Mathf.Max(mainCamera.scaledPixelWidth, 1), Mathf.Max(mainCamera.scaledPixelHeight, 1),
                mainCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, 24)
            {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };
        descriptor.width = Mathf.Max(mainCamera.scaledPixelWidth, 1);
        descriptor.height = Mathf.Max(mainCamera.scaledPixelHeight, 1);
        descriptor.depthBufferBits = 24;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.useDynamicScale = false;
        descriptor.useDynamicScaleExplicit = false;
        descriptor.enableRandomWrite = false;
        EnsureTextures(descriptor);
        if (colorName != null) owner.TextureBindings.Set(this, colorName, ColorTexture);
        if (depthName != null) owner.TextureBindings.Set(this, depthName, DepthTexture);
        Camera.targetTexture = ColorTexture;
        if (DepthTexture != null)
        {
            if (depthCapture == null)
            {
                depthCapture = new CommandBuffer { name = "Vivify Opaque Depth Capture" };
                Camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthCapture);
            }
            depthCapture.Clear();
            owner.RecordDepthCapture(depthCapture, this);
        }
    }

    private void EnsureTextures(RenderTextureDescriptor descriptor)
    {
        if (Matches(ColorTexture, descriptor)) return;
        ReleaseTextures();
        ColorTexture = new RenderTexture(descriptor)
        {
            name = gameObject.name + " Color",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        ColorTexture.Create();
        if (depthName == null) return;
        descriptor.depthBufferBits = 0;
        descriptor.depthStencilFormat = GraphicsFormat.None;
        descriptor.colorFormat = RenderTextureFormat.RFloat;
        descriptor.sRGB = false;
        DepthTexture = new RenderTexture(descriptor)
        {
            name = gameObject.name + " Depth",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        DepthTexture.Create();
    }

    private static bool Matches(RenderTexture texture, RenderTextureDescriptor descriptor) =>
        texture != null && texture.width == descriptor.width && texture.height == descriptor.height
        && texture.graphicsFormat == descriptor.graphicsFormat && texture.dimension == descriptor.dimension
        && texture.volumeDepth == descriptor.volumeDepth;

    private void OnPreCull() => owner?.BeginCull(Camera);
    private void OnPostRender() => owner?.EndCull(Camera);

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (owner == null)
            Graphics.Blit(source, destination);
        else
            owner.RenderCapture(this, source, destination);
    }

    private void OnDisable()
    {
        owner?.EndCull(Camera);
    }

    private void OnDestroy() => Release();

    internal void Release()
    {
        owner?.EndCull(Camera);
        if (depthCapture != null)
        {
            if (Camera != null) Camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, depthCapture);
            depthCapture.Release();
            depthCapture = null;
        }
        ReleaseTextures();
        owner = null;
    }

    private void ReleaseTextures()
    {
        depthCapture?.Clear();
        if (Camera != null) Camera.targetTexture = null;
        owner?.TextureBindings.Remove(this, colorName);
        owner?.TextureBindings.Remove(this, depthName);
        ReleaseTexture(ColorTexture);
        ReleaseTexture(DepthTexture);
        ColorTexture = null;
        DepthTexture = null;
    }

    private static void ReleaseTexture(RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Destroy(texture);
    }
}
