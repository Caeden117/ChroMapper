#ifndef CHROMAPPER_POST_PROCESS_INCLUDED
#define CHROMAPPER_POST_PROCESS_INCLUDED

// Shared blue-noise dithering helpers. Generic fog lives in Fog.hlsl; BloomFog
// prepass composition lives in BloomFogComposition.hlsl.
// Lit owns its distance-darkening and rim-light logic.

inline float4 ApplyNoiseDither(
    float4 result, float4 noiseScreenPosition, sampler2D globalBlueNoiseTex)
{
    float2 noiseUv = noiseScreenPosition.xy / noiseScreenPosition.ww;
    float noise = tex2D(globalBlueNoiseTex, noiseUv).r - 0.5;
    result.rgb += noise.xxx * (1.0 / 255.0);
    return result;
}

inline float4 BuildNoiseScreenPosition(
    float4 screenPosition, float4 clipPosition, float2 noiseScale,
    float randomValue, float2 objectTranslation)
{
    screenPosition.xy *= noiseScale;
    screenPosition.xy += clipPosition.w * randomValue + objectTranslation;
    screenPosition.zw = clipPosition.zw;
    return screenPosition;
}

#endif
