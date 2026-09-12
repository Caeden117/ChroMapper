using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MirrorRendererSO", menuName = "Environment/Mirror Renderer")]
public class MirrorRendererSO : ScriptableObject
{
    private const int mirrorBloomResolution = 512;
    private const string depthTextureKeyword = "DEPTH_TEXTURE";
    private const string nativeDepthTextureKeyword = "DEPTH_TEXTURE_ENABLED";

    public enum MirrorQuality
    {
        None,
        Low,
        High
    }

    [SerializeField] private LayerMask reflectLayers = -1;
    private int textureWidth = 1024;
    private int textureHeight = 1024;
    private int maxAntiAliasing = 4;
    private bool disableDepthTexture = true;

    private Camera mirrorCamera;
    private RenderTexture mirrorBloomRaw;
    private RenderTexture mirrorBloomTexture;
    private int antialiasing = 1;
    private MirrorQuality quality = MirrorQuality.High;

    private readonly Dictionary<CameraTransformData, RenderTexture> renderTextures = new(4);
    private readonly Rect fullRect = new(0f, 0f, 1f, 1f);

    private void Awake()
    {
        HandleMirrorQuality(Settings.Instance.MirrorQuality);
        Settings.NotifyBySettingName(nameof(Settings.MirrorQuality), HandleMirrorQuality);
    }

    private void HandleMirrorQuality(object value)
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying) return;
    #endif
        quality = (MirrorQuality)value;
        textureWidth = textureHeight = quality == MirrorQuality.Low ? 256 : 1024;
        antialiasing = quality == MirrorQuality.Low ? 1 : 2;
    }

    private void OnDisable()
    {
        if ((bool)mirrorCamera)
        {
            GameObjectExtensions.DestroySafe(mirrorCamera.gameObject);
            mirrorCamera = null;
        }

        foreach (var value in renderTextures.Values) RenderTexture.ReleaseTemporary(value);

        renderTextures.Clear();
        ReleaseMirrorBloomTextures();
    }

    public void PrepareForNextFrame()
    {
        foreach (var value in renderTextures.Values) RenderTexture.ReleaseTemporary(value);
        renderTextures.Clear();
    }

    public Texture RenderMirrorTexture(Camera cam, Vector3 planePos, Vector3 planeNormal)
    {
        if (quality == MirrorQuality.None) return null;
        if (reflectLayers == 0) return null;
        if (!cam || cam == mirrorCamera) return null;

        var tr = cam.transform;
        var position = tr.position;
        var rotation = tr.rotation;
        var plane = new Plane(planeNormal, planePos);
        if (plane.GetDistanceToPoint(position) <= 0.0001f
            || (cam.orthographic
                && Mathf.Abs(Vector3.Dot(tr.forward, planeNormal)) <= 0.0001f))
            return null;

        var ctd = new CameraTransformData
        {
            Position = position, Rotation = rotation, FOV = cam.fieldOfView, RefPlane = plane
        };
        if (renderTextures.TryGetValue(ctd, out var texture)) return texture;

        var mirrorAntiAliasing = Mathf.Min(
            Mathf.Max(QualitySettings.antiAliasing, 1),
            antialiasing);
        texture = RenderTexture.GetTemporary(
            textureWidth,
            textureHeight,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default,
            mirrorAntiAliasing);
        renderTextures[ctd] = texture;
        CreateOrUpdateMirrorCamera(cam, texture);

        var reflectionMatrix = CalculateReflectionMatrix(Plane(planePos, planeNormal));
        var previousInvertCulling = GL.invertCulling;
        var bloomfogRenderingController = BloomfogRenderingController.Instance;
        var sourceBloomFogState = bloomfogRenderingController == null
            ? default
            : bloomfogRenderingController.CaptureGlobalState();
        var depthTextureWasEnabled = Shader.IsKeywordEnabled(depthTextureKeyword);
        var nativeDepthTextureWasEnabled = Shader.IsKeywordEnabled(nativeDepthTextureKeyword);

        try
        {
            // Depth keywords are global, but the mirror may not produce a depth texture.
            // Suppress both supported aliases through the reflection prepass and scene draw.
            if ((mirrorCamera.depthTextureMode & DepthTextureMode.Depth) == 0)
            {
                Shader.DisableKeyword(depthTextureKeyword);
                Shader.DisableKeyword(nativeDepthTextureKeyword);
            }

            if (bloomfogRenderingController != null
                && bloomfogRenderingController.CanRenderReflections)
            {
                EnsureMirrorBloomTextures();
                bloomfogRenderingController.RenderReflection(
                    cam.worldToCameraMatrix * reflectionMatrix,
                    cam.projectionMatrix,
                    mirrorBloomRaw,
                    mirrorBloomTexture);
            }

            GL.invertCulling = !previousInvertCulling;
            RenderMirror(
                position,
                rotation,
                cam.projectionMatrix,
                fullRect,
                planePos,
                planeNormal,
                reflectionMatrix);
        }
        finally
        {
            if (depthTextureWasEnabled) Shader.EnableKeyword(depthTextureKeyword);
            else Shader.DisableKeyword(depthTextureKeyword);
            if (nativeDepthTextureWasEnabled) Shader.EnableKeyword(nativeDepthTextureKeyword);
            else Shader.DisableKeyword(nativeDepthTextureKeyword);

            GL.invertCulling = previousInvertCulling;
            if (bloomfogRenderingController != null)
                bloomfogRenderingController.RestoreGlobalState(sourceBloomFogState);
            if (PerCameraShaderSetupController.Instance != null)
                PerCameraShaderSetupController.Instance.ApplyCameraState(cam);
        }
        GL.Flush();
        return texture;
    }

    private void RenderMirror(
        Vector3 camPosition,
        Quaternion camRotation,
        Matrix4x4 camProjectionMatrix,
        Rect screenRect,
        Vector3 planePos,
        Vector3 planeNormal,
        Matrix4x4 reflectionMatrix)
    {
        mirrorCamera.rect = screenRect;
        mirrorCamera.projectionMatrix = camProjectionMatrix;

        mirrorCamera.ResetWorldToCameraMatrix();
        mirrorCamera.transform.SetPositionAndRotation(camPosition, camRotation);

        var worldToCameraMatrix = mirrorCamera.worldToCameraMatrix;
        worldToCameraMatrix *= reflectionMatrix;
        mirrorCamera.worldToCameraMatrix = worldToCameraMatrix;

        var clipPlane = CameraSpacePlane(worldToCameraMatrix, planePos, planeNormal);
        mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(clipPlane);

        mirrorCamera.Render();
    }

    private void EnsureMirrorBloomTextures()
    {
        var format = BloomRenderUtility.GetBloomTextureFormat();
        if (mirrorBloomRaw != null
            && mirrorBloomTexture != null
            && mirrorBloomRaw.width == mirrorBloomResolution
            && mirrorBloomRaw.height == mirrorBloomResolution
            && mirrorBloomRaw.format == format
            && mirrorBloomTexture.format == format
            && mirrorBloomRaw.IsCreated()
            && mirrorBloomTexture.IsCreated())
            return;

        ReleaseMirrorBloomTextures();
        mirrorBloomRaw = CreateMirrorBloomTexture("Mirror Bloomfog Raw", format);
        mirrorBloomTexture = CreateMirrorBloomTexture("Mirror Bloomfog Final", format);
    }

    private static RenderTexture CreateMirrorBloomTexture(string textureName, RenderTextureFormat format)
    {
        var texture = new RenderTexture(
            mirrorBloomResolution,
            mirrorBloomResolution,
            0,
            format,
            RenderTextureReadWrite.Linear)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.Create();
        return texture;
    }

    private void ReleaseMirrorBloomTextures()
    {
        ReleaseOwnedTexture(ref mirrorBloomRaw);
        ReleaseOwnedTexture(ref mirrorBloomTexture);
    }

    private static void ReleaseOwnedTexture(ref RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        GameObjectExtensions.DestroySafe(texture);
        texture = null;
    }

    private void CreateOrUpdateMirrorCamera(Camera cam, RenderTexture renderTexture)
    {
        if (!mirrorCamera)
        {
            var go = new GameObject(
                "MirrorCam" + GetInstanceID(),
                typeof(Camera),
                typeof(CameraDisableUIRendering),
                typeof(MirrorCamera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            mirrorCamera = go.GetComponent<Camera>();
            mirrorCamera.enabled = false;
        }

        mirrorCamera.CopyFrom(cam);
        mirrorCamera.targetTexture = renderTexture;
        if (disableDepthTexture) mirrorCamera.depthTextureMode = DepthTextureMode.None;

        mirrorCamera.cullingMask = -17 & reflectLayers.value & cam.cullingMask;
        mirrorCamera.clearFlags = CameraClearFlags.Color;
    }

    private static Vector4 Plane(Vector3 pos, Vector3 normal) =>
        new(normal.x, normal.y, normal.z, 0f - Vector3.Dot(pos, normal));

    private static Vector4 CameraSpacePlane(Matrix4x4 worldToCameraMatrix, Vector3 pos, Vector3 normal)
    {
        var targetPos = worldToCameraMatrix.MultiplyPoint(pos);
        var normalized = worldToCameraMatrix.MultiplyVector(normal).normalized;

        return Plane(targetPos, normalized);
    }

    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        var identity = Matrix4x4.identity;

        identity.m00 = 1f - (2f * plane[0] * plane[0]);
        identity.m01 = -2f * plane[0] * plane[1];
        identity.m02 = -2f * plane[0] * plane[2];
        identity.m03 = -2f * plane[3] * plane[0];

        identity.m10 = -2f * plane[1] * plane[0];
        identity.m11 = 1f - (2f * plane[1] * plane[1]);
        identity.m12 = -2f * plane[1] * plane[2];
        identity.m13 = -2f * plane[3] * plane[1];

        identity.m20 = -2f * plane[2] * plane[0];
        identity.m21 = -2f * plane[2] * plane[1];
        identity.m22 = 1f - (2f * plane[2] * plane[2]);
        identity.m23 = -2f * plane[3] * plane[2];

        identity.m30 = 0f;
        identity.m31 = 0f;
        identity.m32 = 0f;
        identity.m33 = 1f;

        return identity;
    }

    private struct CameraTransformData : IEquatable<CameraTransformData>
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float FOV;
        public Plane RefPlane;

        public bool Equals(CameraTransformData other)
        {
            if (Position == other.Position
                && Rotation == other.Rotation
                && FOV == other.FOV
                && RefPlane.distance == other.RefPlane.distance)
                return RefPlane.normal == other.RefPlane.normal;

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is CameraTransformData other) return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = default(HashCode);
            hashCode.Add(Position);
            hashCode.Add(Rotation);
            hashCode.Add(FOV);
            hashCode.Add(RefPlane.distance);
            hashCode.Add(RefPlane.normal);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(CameraTransformData left, CameraTransformData right) => left.Equals(right);
        public static bool operator !=(CameraTransformData left, CameraTransformData right) => !left.Equals(right);
    }
}
