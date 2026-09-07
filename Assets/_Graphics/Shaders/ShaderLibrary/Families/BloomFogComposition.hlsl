#ifndef CHROMAPPER_RECOVERED_BLOOM_FOG_COMPOSITION_INCLUDED
#define CHROMAPPER_RECOVERED_BLOOM_FOG_COMPOSITION_INCLUDED

#include "../Common/Fog.hlsl"

float2 _CustomFogTextureToScreenRatio;
sampler2D _BloomPrePassTexture;

inline float4 SampleBloomPrePass(float4 screenPos)
{
    #if defined(BLOOM_FOG)
    float2 customFogUV = screenPos.xy / screenPos.w;
    customFogUV = (customFogUV + -0.5) * _CustomFogTextureToScreenRatio + 0.5;
    return float4(tex2D(_BloomPrePassTexture, customFogUV).rgb, 0);
    #else
    return 0;
    #endif
}

inline float4 BlendFogColor(float4 col, float4 bloomfogCol)
{
    #if defined(_FOGTYPE_ALPHA)
    col.a = bloomfogCol.a;
    return col;
    #elif defined(_FOGTYPE_COLOR)
    col.rgb = bloomfogCol.rgb;
    return col;
    #else
    return bloomfogCol;
    #endif
}

inline float4 ApplyBloomFogCalculatedFactor(float4 col, float4 screenPos, float fogFactor)
{
    float4 bloomPrepassCol = SampleBloomPrePass(screenPos);
    float4 bloomfogCol = fogFactor * (-col + bloomPrepassCol) + col;
    return BlendFogColor(col, bloomfogCol);
}

inline float4 ApplyBloomFog(float4 col, float4 screenPos, float3 worldPos, float fogStartOffset, float fogScale)
{
    return ApplyBloomFogCalculatedFactor(
        col, screenPos, CalculateCustomFogFactor(distanceSquared(worldPos), fogStartOffset, fogScale));
}

inline float4 ApplyBloomHeightFogCalculatedFactor(float4 col, float4 screenPos, float fogFactor, float heightFogFactor)
{
    float4 bloomPrepassCol = SampleBloomPrePass(screenPos);
    fogFactor = -fogFactor + 1;
    float4 bloomfogCol = (heightFogFactor * -fogFactor + 1) * (-col + bloomPrepassCol) + col;
    return BlendFogColor(col, bloomfogCol);
}

inline float4 ApplyBloomHeightFog(float4 col, float4 screenPos, float3 worldPos, float fogStartOffset, float fogScale,
                                  float fogHeightOffset, float fogHeightScale)
{
    return ApplyBloomHeightFogCalculatedFactor(
        col, screenPos,
        CalculateCustomFogFactor(distanceSquared(worldPos), fogStartOffset, fogScale),
        CalculateCustomHeightFogFactor(worldPos, fogHeightOffset, fogHeightScale));
}

#endif // CHROMAPPER_RECOVERED_BLOOM_FOG_COMPOSITION_INCLUDED
