#ifndef CHROMAPPER_CUTOUT_INCLUDED
#define CHROMAPPER_CUTOUT_INCLUDED
// Uses world axes relative to objectOrigin; this is not an inverse rotation or scale transform.
inline float3 CalculateObjectSpaceCutoutPosition(
    float3 worldPosition, float3 objectOrigin, float3 textureOffset, float textureScale)
{
    return (worldPosition - objectOrigin + textureOffset) * textureScale;
}

// Discards when noiseSample - 1.1 * cutout + 0.1 < 0.
inline void ApplyCutoutNoise(float noiseSample, float cutout)
{
    clip(noiseSample - 1.1 * cutout + 0.1);
}

#endif
