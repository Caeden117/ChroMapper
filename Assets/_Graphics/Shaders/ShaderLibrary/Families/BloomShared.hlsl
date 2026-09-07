#ifndef CHROMAPPER_BLOOM_SHARED_INCLUDED
#define CHROMAPPER_BLOOM_SHARED_INCLUDED

// Unity 6 does not provide these texture macros in every built-in pipeline
// HLSLPROGRAM block. Define the small fallback set when the engine does not
// provide it.
#ifndef TEXTURE2D
#define TEXTURE2D(textureName) Texture2D textureName
#endif
#ifndef SAMPLER
#define SAMPLER(samplerName) SamplerState samplerName
#endif
#ifndef TEXTURE2D_ARGS
#define TEXTURE2D_ARGS(textureName, samplerName) Texture2D textureName, SamplerState samplerName
#endif
#ifndef TEXTURE2D_PARAM
#define TEXTURE2D_PARAM(textureName, samplerName) textureName, samplerName
#endif
#ifndef TEXTURE2D_SAMPLER2D
#define TEXTURE2D_SAMPLER2D(textureName, samplerName) TEXTURE2D(textureName); SAMPLER(samplerName)
#endif
#ifndef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) textureName.Sample(samplerName, coord2)
#endif
#ifndef BLOOM_SAMPLE_UV
#define BLOOM_SAMPLE_UV(coord) saturate(coord)
#endif

#include "../Core/Tonemapping.hlsl"

// Recovered four-tap box downsample.
inline float4 BloomDownsample4(
    TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
    float4 color = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.xy));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.zy));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.xw));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.zw));
    return color * 0.25;
}

// Recovered classic 13-tap downsample. The four half-texel taps use weight
// 1/8. The 3x3 taps use weights 1/32, 1/16, and 1/8.
inline float4 BloomDownsample13Classic(
    TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize)
{
    float4 a0 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 0.5,  0.5)));
    float4 a1 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2(-0.5,  0.5)));
    float4 a2 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 0.5, -0.5)));
    float4 a3 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2(-0.5, -0.5)));

    float4 c0 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2(-1, -1)));
    float4 c1 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 0, -1)));
    float4 c2 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 1, -1)));
    float4 c3 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2(-1,  0)));
    float4 c4 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv));
    float4 c5 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 1,  0)));
    float4 c6 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2(-1,  1)));
    float4 c7 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 0,  1)));
    float4 c8 = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + texelSize * float2( 1,  1)));

    return (a0 + a1 + a2 + a3) * 0.125
         + (c0 + c2 + c6 + c8) * 0.03125
         + (c1 + c3 + c5 + c7) * 0.0625
         + c4 * 0.125;
}

// Recovered nine-tap bilinear tent upsample.
inline float4 BloomUpsampleTent(
    TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;
    float4 color = SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv - d.xy));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv - d.wy)) * 2.0;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv - d.zy));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.zw)) * 2.0;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv)) * 4.0;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.xw)) * 2.0;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.zy));
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.wy)) * 2.0;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, BLOOM_SAMPLE_UV(uv + d.xy));
    return color * (1.0 / 16.0);
}

// Recovered four-tap box upsample.
inline float4 BloomUpsampleBox(
    TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale)
{
    float2 offset = texelSize * (sampleScale * 0.5);
    float4 color = SAMPLE_TEXTURE2D(
        tex, samplerTex, BLOOM_SAMPLE_UV(uv + float2(-offset.x, -offset.y)));
    color += SAMPLE_TEXTURE2D(
        tex, samplerTex, BLOOM_SAMPLE_UV(uv + float2(offset.x, -offset.y)));
    color += SAMPLE_TEXTURE2D(
        tex, samplerTex, BLOOM_SAMPLE_UV(uv + float2(-offset.x, offset.y)));
    color += SAMPLE_TEXTURE2D(
        tex, samplerTex, BLOOM_SAMPLE_UV(uv + offset));
    return color * 0.25;
}

inline float4 BloomWeightedCombine(
    float4 source, float4 destination, float sourceWeight, float destinationWeight)
{
    return source * sourceWeight + destination * destinationWeight;
}

inline float4 BloomAlphaGate(float4 color, float alphaWeights)
{
    color.rgb *= saturate(color.a * alphaWeights);
    return color;
}

// Rec601 luminance drives the recovered auto-exposure knee.
inline float BloomRec601AutoExposureKnee(
    float3 globalIntensity, float autoExposureLimit, float legacyAutoExposure)
{
    // Keep the exposure-cap denominator positive. Preserve zero luminance for
    // the legacy exposure calculation so a black probe still produces zero.
    float luminance = dot(globalIntensity, float3(0.3, 0.59, 0.11));
    float exposureCap = 0.1 / sqrt(luminance);
    return legacyAutoExposure > 0.0
        ? min(luminance * autoExposureLimit, exposureCap)
        : min(0.004 * autoExposureLimit, exposureCap);
}

inline float4 BloomApplyKneeAndAces(
    float4 color, float3 globalIntensity, float autoExposureLimit, float legacyAutoExposure)
{
    color *= BloomRec601AutoExposureKnee(
        globalIntensity, autoExposureLimit, legacyAutoExposure);
    return ApplyAcesTonemapping(color);
}

// Recovered cubic approximation used by the gamma upsample variants.
inline float4 BloomApplyGamma(float4 color)
{
    color.rgb *= color.rgb * (0.305306011 * color.rgb + 0.682171111) + 0.012522878;
    return color;
}
#endif
