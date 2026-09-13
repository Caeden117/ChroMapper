#ifndef CHROMAPPER_FOG_INCLUDED
#define CHROMAPPER_FOG_INCLUDED

#include "../Core/Camera.hlsl"

// These are the global variable names the game uses by default,
// certain mods might want to use their own attenuation/offset variable names.
#ifndef CUSTOM_FOG_ATTENUATION_NAME
#define CUSTOM_FOG_ATTENUATION_NAME _CustomFogAttenuation
#endif
#ifndef CUSTOM_FOG_OFFSET_NAME
#define CUSTOM_FOG_OFFSET_NAME _CustomFogOffset
#endif

float CUSTOM_FOG_ATTENUATION_NAME;
float CUSTOM_FOG_OFFSET_NAME;

inline float distanceSquared(float3 pos)
{
    float3 distance = pos - GetStereoAwareCameraPosition();
    return dot(distance, distance);
}

inline float CalculateCustomFogFactor(float distanceSq, float fogStartOffset, float fogScale)
{
    float result = max(distanceSq + -fogStartOffset, 0);
    result = max(result * fogScale + -CUSTOM_FOG_OFFSET_NAME, 0);
    result = 1 / (result * CUSTOM_FOG_ATTENUATION_NAME + 1);
    return -result + 1;
}

#ifndef CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME
#define CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME _CustomFogHeightFogStartY
#endif
#ifndef CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME
#define CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME _CustomFogHeightFogHeight
#endif

float CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
float CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME;

inline float CalculateCustomHeightFogFactor(float3 worldPos, float fogHeightOffset, float fogHeightScale)
{
    float result = CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME + CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
    result = ((worldPos.y * fogHeightScale) + fogHeightOffset) + -result;
    result = clamp(result / CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME, 0, 1);
    return (-result * 2 + 3) * (result * result);
}

// Generic fog helpers. Custom fog globals above are owned by this include;
// BloomFog prepass globals and composition are owned by
// BloomFogComposition.hlsl.
// Per-material fog inputs are passed as arguments.

inline float CalculateHeightFogFactor(float exactHeightInput)
{
    exactHeightInput -= CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME + CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
    exactHeightInput = saturate(exactHeightInput / CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME);
    return 1.0 -
        exactHeightInput * exactHeightInput * (3.0 - 2.0 * exactHeightInput);
}

inline float4 ApplyColorFog(
    float4 result, float3 worldPosition,
    float colorFogMultiplier, float colorFogMax, float colorFogHighlightMultiplier,
    float colorFogInfluence, float fogHeightScale, float fogHeightOffset)
{
    float resolvedColorFogMultiplier = min(0.0001 * colorFogMultiplier, colorFogMax);
    #if defined(FOG_COLOR_HIGHLIGHT)
    float colorFogHighlight = min(
        0.1 * colorFogHighlightMultiplier * (1.0 + resolvedColorFogMultiplier),
        colorFogMax);
    #else
    float colorFogHighlight = 0.0;
    #endif
    float4 colorFogResult = float4(
        result.rgb * colorFogInfluence + colorFogHighlight,
        result.a);
    #if defined(BLOOM_FOG) && defined(FOG)
    return colorFogResult;
    #else
    float exactHeightInput = worldPosition.y * fogHeightScale + fogHeightOffset;
    float exactHeightFog = CalculateHeightFogFactor(exactHeightInput);
    return exactHeightFog.xxxx *
        (float4(colorFogHighlight.xxx, 0.0) - colorFogResult) + colorFogResult;
    #endif
}

#endif // CHROMAPPER_FOG_INCLUDED
