#ifndef CHROMAPPER_PARAMETRIC_SHARED_INCLUDED
#define CHROMAPPER_PARAMETRIC_SHARED_INCLUDED

#include "../Core/Camera.hlsl"
// ParametricBoxOpaque intentionally bypasses this helper for its distinct opaque camera path.
inline float3 GetParametricCameraPosition()
{
    return GetStereoAwareCameraPosition();
}

inline float CalculateParametricHeightRamp(
    float worldY, float heightScale, float heightOffset,
    float globalHeight, float globalStartY)
{
    float height = saturate(
        (worldY * heightScale + heightOffset - (globalHeight + globalStartY)) /
        max(globalHeight, 1e-5));
    return height * height * (3.0 - 2.0 * height);
}

inline float CalculateParametricDistanceTransmission(
    float3 worldPosition, float3 cameraPosition, float fogStartOffset,
    float fogScale, float alphaDivisor, float fogOffset, float fogAttenuation)
{
    float3 toFragment = worldPosition - cameraPosition;
    float distanceSq = dot(toFragment, toFragment);
    float fogDistance = max(distanceSq - fogStartOffset, 0.0);
    float density = max(fogDistance * (fogScale / max(alphaDivisor, 1.0)) - fogOffset, 0.0);
    return rcp(density * fogAttenuation + 1.0);
}

inline float3 WarpParametricNoisePosition(
    float3 noisePosition, float zoomStrength, float skewStrength)
{
    float denominator = abs(noisePosition.z * zoomStrength) + 1.0;
    float2 warpedPosition = float2(
        noisePosition.x + noisePosition.y * skewStrength / denominator,
        noisePosition.y - noisePosition.x * skewStrength / denominator) / denominator;
    return float3(warpedPosition, noisePosition.z);
}

inline float3 CalculateParametricNoiseUv(
    float3 worldPosition, float3 scrolling, float timeValue, float scale,
    float zoomStrength, float skewStrength, float applyWarp)
{
    float3 noisePosition = worldPosition;
    if (applyWarp > 0.5)
        noisePosition = WarpParametricNoisePosition(noisePosition, zoomStrength, skewStrength);
    return (scrolling * timeValue + noisePosition) * scale;
}

inline float CalculateParametricWorldFade(float worldY, float fadePosition, float fadeSlope)
{
    return saturate((worldY - fadePosition) * fadeSlope);
}

inline float SampleParametricWorldNoise(
    float3 worldPosition, sampler3D noiseTexture, float3 scrolling, float timeValue,
    float scale, float intensityOffset, float intensityScale,
    float fadePosition, float fadeSlope,
    float warpZoomStrength, float warpSkewStrength)
{
    float3 noiseUv = CalculateParametricNoiseUv(
        worldPosition, scrolling, timeValue, scale,
        warpZoomStrength, warpSkewStrength,
        #if defined(WORLD_NOISE_WARP)
        1.0
        #else
        0.0
        #endif
    );
    float noise = tex3D(noiseTexture, noiseUv).w * intensityScale + intensityOffset;
    #if defined(WORLD_SPACE_FADE)
    noise *= CalculateParametricWorldFade(worldPosition.y, fadePosition, fadeSlope);
    #endif
    return noise;
}

#endif
