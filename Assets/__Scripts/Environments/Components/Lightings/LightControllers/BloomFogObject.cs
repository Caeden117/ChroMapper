using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BloomFogObject : MonoBehaviour
{
    public static List<BloomFogObject> AllBloomFogLights = new();

    public float Length = 1f;
    public float Center = 1f;

    public float StartWidth = 1f;
    public float EndWidth = 1f;

    public float StartAlpha = 1f;
    public float EndAlpha = 1f;

    public float MultiplyLengthByAlphaBloomFogMultiplier = 1f;
    public float MultiplyLengthByAlphaMultiplier = 1f;
    public float LightWidthMultiplier = 1f;
    public float IntensityMultiplier = 1f;

    public float BoostToWhite;
    public bool DisableRenderersOnZeroAlpha;
    public bool LimitAlpha;
    public float MinAlpha;
    public float MaxAlpha = 1f;

    [NonSerialized] public Transform CachedTransform;
    private Color color;

    private void OnEnable()
    {
        // Environment objects can be re-enabled during scene transitions
        // without a matching disable callback. Keep one render entry per light.
        if (!AllBloomFogLights.Contains(this)) AllBloomFogLights.Add(this);
    }

    private void OnDisable()
    {
        // Remove all stale entries left by older enable cycles.
        while (AllBloomFogLights.Remove(this)) { }
    }

    public void SetColor(Color col) => color = col;

    public void ApplyToQuad(
        ref int quadNum,
        BloomfogQuad[] quads,
        Matrix4x4 view,
        Matrix4x4 projection,
        float lineWidth)
    {
        // Get current quad
        ref var quad = ref quads[quadNum];

        if (DisableRenderersOnZeroAlpha && color.a < 0.01f)
        {
            ZeroQuad(ref quad);
            return;
        }

        quadNum++;

        // Calculate tube start/end based on center and length
        var length = Length;
        var startLocalY = -length * MultiplyLengthByAlphaBloomFogMultiplier * Center;
        var endLocalY = length * MultiplyLengthByAlphaBloomFogMultiplier * (1f - Center);

        // Calculate endpoints in world space
        var localToWorld = CachedTransform.localToWorldMatrix;
        var tubeStartWorld = localToWorld.MultiplyPoint3x4(new Vector3(0f, startLocalY, 0f));
        var tubeEndWorld = localToWorld.MultiplyPoint3x4(new Vector3(0f, endLocalY, 0f));

        // Transform to view space
        var startView = view.MultiplyPoint3x4(tubeStartWorld);
        var endView = view.MultiplyPoint3x4(tubeEndWorld);

        // Transform to clip space
        var startClip = projection * new Vector4(startView.x, startView.y, startView.z, 1);
        var endClip = projection * new Vector4(endView.x, endView.y, endView.z, 1);

        #region Frustrum Culling

        // Left frustrum
        var startPointInsideFrustrum = startClip.x >= -startClip.w;
        var endPointInsideFrustrum = endClip.x >= -endClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var leftFrustumClipInterpolation = (-startClip.w - startClip.x)
                / (endClip.x - startClip.x + endClip.w - startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                leftFrustumClipInterpolation);
        }

        // Right frustrum
        startPointInsideFrustrum = startClip.x <= startClip.w;
        endPointInsideFrustrum = endClip.x <= endClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var rightFrustumClipInterpolation = (startClip.w - startClip.x)
                / (endClip.x - startClip.x - endClip.w + startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                rightFrustumClipInterpolation);
        }

        // Bottom frustrum
        startPointInsideFrustrum = startClip.y >= -startClip.w;
        endPointInsideFrustrum = endClip.y >= -endClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var bottomFrustumClipInterpolation = (-startClip.w - startClip.y)
                / (endClip.y - startClip.y + endClip.w - startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                bottomFrustumClipInterpolation);
        }

        // Top frustrum
        startPointInsideFrustrum = startClip.y <= startClip.w;
        endPointInsideFrustrum = endClip.y <= endClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var topFrustumClipInterpolation = (startClip.w - startClip.y)
                / (endClip.y - startClip.y - endClip.w + startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                topFrustumClipInterpolation);
        }

        // Far plane
        startPointInsideFrustrum = startClip.z <= startClip.w;
        endPointInsideFrustrum = endClip.z <= endClip.w;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var farPlaneClipInterpolation = (startClip.w - startClip.z)
                / (endClip.z - startClip.z - endClip.w + startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                farPlaneClipInterpolation);
        }

        // Near plane (with small epsilon for precision)
        startPointInsideFrustrum = startClip.z >= -startClip.w - 0.0001f;
        endPointInsideFrustrum = endClip.z >= -endClip.w - 0.0001f;
        if (!startPointInsideFrustrum && !endPointInsideFrustrum)
        {
            ZeroQuad(ref quad);
            return;
        }

        if (startPointInsideFrustrum != endPointInsideFrustrum)
        {
            var nearPlaneClipInterpolation = (-startClip.w - startClip.z)
                / (endClip.z - startClip.z + endClip.w - startClip.w);
            ClipPoints(
                ref startClip,
                ref endClip,
                ref startView,
                ref endView,
                startPointInsideFrustrum,
                nearPlaneClipInterpolation);
        }

        #endregion

        // Convert to NDC space
        var startScreenX = (startClip.x / startClip.w * 0.5f) + 0.5f;
        var startScreenY = (startClip.y / startClip.w * 0.5f) + 0.5f;
        var endScreenX = (endClip.x / endClip.w * 0.5f) + 0.5f;
        var endScreenY = (endClip.y / endClip.w * 0.5f) + 0.5f;

        // Calculate screen space direction
        var screenDirX = endScreenX - startScreenX;
        var screenDirY = endScreenY - startScreenY;
        var screenDirLength = Mathf.Sqrt((screenDirX * screenDirX) + (screenDirY * screenDirY));

        // Keep sub-epsilon segments from expanding to full-width quads.
        screenDirLength = Mathf.Max(screenDirLength, 1E-06f);

        // Normalize direction
        screenDirX /= screenDirLength;
        screenDirY /= screenDirLength;

        // Apply anti-aliasing offset
        var screenOffsetX = screenDirX * (1f / 64);
        var screenOffsetY = screenDirY * (1f / 64);
        endScreenX += screenOffsetX;
        endScreenY += screenOffsetY;
        startScreenX -= screenOffsetX;
        startScreenY -= screenOffsetY;

        // Calculate perpendicular direction
        var effectiveLineWidth = lineWidth * LightWidthMultiplier;
        var perpX = -screenDirY * effectiveLineWidth;
        var perpY = screenDirX * effectiveLineWidth;

        // Calculate width offsets at endpoints
        var startWidthOffsetX = perpX * StartWidth;
        var startWidthOffsetY = perpY * StartWidth;
        var endWidthOffsetX = perpX * EndWidth;
        var endWidthOffsetY = perpY * EndWidth;

        // Calculate color components
        var boostedR = color.r + BoostToWhite;
        var boostedG = color.g + BoostToWhite;
        var boostedB = color.b + BoostToWhite;
        var finalAlpha = color.a * IntensityMultiplier;

        if (LimitAlpha) finalAlpha = Mathf.Clamp(finalAlpha, MinAlpha, MaxAlpha);
        finalAlpha = Mathf.LinearToGammaSpace(finalAlpha);

        var startAlpha = StartAlpha;
        var endAlpha = EndAlpha * MultiplyLengthByAlphaMultiplier;

        // Calculate vertex colors
        var startColor = new Color(
            startAlpha * boostedR,
            startAlpha * boostedG,
            startAlpha * boostedB,
            startAlpha * finalAlpha);
        var endColor = new Color(
            endAlpha * boostedR,
            endAlpha * boostedG,
            endAlpha * boostedB,
            endAlpha * finalAlpha);

        // Fill quad data
        quad.Vertex0Position.x = startScreenX - startWidthOffsetX;
        quad.Vertex0Position.y = startScreenY - startWidthOffsetY;
        quad.Vertex0Position.z = 0;
        quad.Vertex0ViewPos = startView;
        quad.Vertex0Color = startColor;
        quad.Vertex0UV = new Vector3(0, 0, StartWidth);

        quad.Vertex1Position.x = startScreenX + startWidthOffsetX;
        quad.Vertex1Position.y = startScreenY + startWidthOffsetY;
        quad.Vertex1Position.z = 0;
        quad.Vertex1ViewPos = startView;
        quad.Vertex1Color = startColor;
        quad.Vertex1UV = new Vector3(StartWidth, 0, StartWidth);

        quad.Vertex2Position.x = endScreenX + endWidthOffsetX;
        quad.Vertex2Position.y = endScreenY + endWidthOffsetY;
        quad.Vertex2Position.z = 0;
        quad.Vertex2ViewPos = endView;
        quad.Vertex2Color = endColor;
        quad.Vertex2UV = new Vector3(EndWidth, 1, EndWidth);

        quad.Vertex3Position.x = endScreenX - endWidthOffsetX;
        quad.Vertex3Position.y = endScreenY - endWidthOffsetY;
        quad.Vertex3Position.z = 0;
        quad.Vertex3ViewPos = endView;
        quad.Vertex3Color = endColor;
        quad.Vertex3UV = new Vector3(0, 1, EndWidth);
    }

    /*private void OnDrawGizmosSelected()
    {
        Debug.Log(color);
    }*/

    private static void ZeroQuad(ref BloomfogQuad quad) => quad = default;

    // Clip the line segment against a single frustum plane
    // Near-plane tolerance can put the exact plane intersection outside the segment.
    private static void ClipPoints(
        ref Vector4 startClipPos,
        ref Vector4 endClipPos,
        ref Vector3 startViewPos,
        ref Vector3 endViewPos,
        bool startPointInsideFrustrum,
        float clipInterpolation)
    {
        if (startPointInsideFrustrum)
        {
            // Start point is inside, end point is outside - clip the end point
            endClipPos = Vector4.LerpUnclamped(startClipPos, endClipPos, clipInterpolation);
            endViewPos = Vector3.LerpUnclamped(startViewPos, endViewPos, clipInterpolation);
        }
        else
        {
            // End point is inside, start point is outside - clip the start point
            startClipPos = Vector4.LerpUnclamped(startClipPos, endClipPos, clipInterpolation);
            startViewPos = Vector3.LerpUnclamped(startViewPos, endViewPos, clipInterpolation);
        }
    }
}
