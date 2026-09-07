// ChroMapper post-bloom compositor. Replacement for the Beat Saber game
// shader Hidden/MainEffect.
Shader "ChroMapper/Post Process/Post Bloom"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3 Hidden/MainEffect)
    // ME1. The recovered corpus contains 16 keyword variants over 7 unique
    //      binaries: 2 vertex (mono 459f777b756340f9, stereo 96bbc8d772fc57ed)
    //      and 4 fragment (base 795279359b297b74, mono clear 2d8150c9bcaac458,
    //      stereo clear 2a39b58b76b55f8d, stereo 750826b6491d4a9d).
    // ME2. LIV_MR never changes a binary. It cancels CLEAR_SCREEN_ALPHA:
    //      CLEAR+LIV_MR maps to the base binary in both eyes.
    // ME3. CLEAR_SCREEN_ALPHA writes output alpha 1.0, not 0. The clear route
    //      keeps RGB composition identical to the base route.
    // ME4. The blue-noise lookup adds (0.1, 0.2) before the parameter scale.
    //      The recovered form is (uv + (0.1, 0.2)) * params + random.
    // ME5. The recovered vertex emits a second UV whose y is flipped when the
    //      source texel-size y is negative. Main taps use v1.xy; the bloom tap
    //      uses v1.zw with that conditional flip.
    // ME6. The recovered vertex applies a source scale/offset transform
    //      (cbuffer slot without a recovered name) before the flip logic. The
    //      ChroMapper blit path passes Unity's quad UV through unchanged, which
    //      matches the identity transform the game's chain supplies.
    // ME7. Every main and bloom tap uses SampleBias with an unnamed cbuffer
    //      bias slot. No ChroMapper producer drives that slot, so plain Sample
    //      at bias 0 is the represented behavior here.
    // ME8. The stereo route samples Texture2DArray sources with the eye index
    //      as the array layer and renders per-eye instances. The blue-noise
    //      texture stays a plain Texture2D in both routes.
    // ME9. The composition order is bloom*intensity + dither, plus scene,
    //      times fade. Output alpha is the center-tap alpha outside the clear
    //      route and is never multiplied by fade.

    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _BloomIntensity ("Bloom Intensity", Float) = 1
        _Fade ("Fade", Float) = 1
    }

    HLSLINCLUDE
#ifndef CHROMAPPER_RECOVERED_POST_BLOOM_PASS_INCLUDED
#define CHROMAPPER_RECOVERED_POST_BLOOM_PASS_INCLUDED

#include "UnityCG.cginc"
#include "../ShaderLibrary/Common/Bloom.hlsl"

struct AppData
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsDefault
{
    float4 vertex : SV_POSITION;
    // xy carries the main UV. zw duplicates it for the conditionally
    // flipped bloom coordinate (ME5).
    float4 texcoord : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsDefault VertDefault(AppData input)
{
    VaryingsDefault output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.vertex = UnityObjectToClipPos(input.vertex);
    output.texcoord = float4(input.texcoord.xy, input.texcoord.xy);
    return output;
}

#if defined(STEREO_INSTANCING_ON)
Texture2DArray _MainTex;
SamplerState sampler_MainTex;
Texture2DArray _PostBloomTexture;
SamplerState sampler_PostBloomTexture;
#else
Texture2D _MainTex;
SamplerState sampler_MainTex;
Texture2D _PostBloomTexture;
SamplerState sampler_PostBloomTexture;
#endif
Texture2D _GlobalBlueNoiseTex;
SamplerState sampler_GlobalBlueNoiseTex;

float4 _PostBloomSourceTexelSize;
float2 _GlobalBlueNoiseParams;
float _GlobalRandomValue;
float _BloomIntensity;
float _Fade;

float4 FragMainEffect(VaryingsDefault input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 texelSize = _PostBloomSourceTexelSize.xy;
    float2 uv = input.texcoord.xy;

    // Recovered bloom coordinate: same x as the main UV, y inverted when
    // the source render target is flagged flipped by its texel size (ME5).
    float2 bloomUv = input.texcoord.zw;
    bloomUv.y = _PostBloomSourceTexelSize.y < 0.0 ? 1.0 - bloomUv.y : bloomUv.y;

    #if defined(STEREO_INSTANCING_ON)
    float3 mainCoord = float3(uv, unity_StereoEyeIndex);
    float3 bloomCoord = float3(bloomUv, unity_StereoEyeIndex);
    float4 center = _MainTex.Sample(sampler_MainTex, mainCoord);
    float alpha =
        _MainTex.Sample(sampler_MainTex, mainCoord + float3(texelSize * float2(-0.5, 0.5), 0.0)).a +
        _MainTex.Sample(sampler_MainTex, mainCoord + float3(texelSize * float2(0.0, -0.5), 0.0)).a +
        _MainTex.Sample(sampler_MainTex, mainCoord + float3(texelSize * float2(0.5, 0.5), 0.0)).a +
        center.a;
    float3 bloom = _PostBloomTexture.Sample(
        sampler_PostBloomTexture, bloomCoord).rgb * _BloomIntensity;
    #else
    float4 center = _MainTex.Sample(sampler_MainTex, uv);
    float alpha =
        _MainTex.Sample(sampler_MainTex, uv + texelSize * float2(-0.5, 0.5)).a +
        _MainTex.Sample(sampler_MainTex, uv + texelSize * float2(0.0, -0.5)).a +
        _MainTex.Sample(sampler_MainTex, uv + texelSize * float2(0.5, 0.5)).a +
        center.a;
    float3 bloom = _PostBloomTexture.Sample(
        sampler_PostBloomTexture, bloomUv).rgb * _BloomIntensity;
    #endif

    // Four-tap alpha average, squared into the white boost (base fragment
    // r0.w chain: mean, square, mad with boost and threshold).
    alpha *= 0.25;
    alpha *= alpha;
    float whiteBoost = alpha * _BaseColorBoost - _BaseColorBoostThreshold;
    float3 scene = saturate(center.rgb + whiteBoost);

    // Blue-noise dither with the recovered pre-offset (ME4).
    float2 noiseUv = (uv + float2(0.1, 0.2)) * _GlobalBlueNoiseParams
        + _GlobalRandomValue;
    float dither =
        (_GlobalBlueNoiseTex.SampleLevel(sampler_GlobalBlueNoiseTex, noiseUv, 0.0).r - 0.5)
        / 255.0;

    // Recovered order: bloom + dither, plus scene, times fade (ME9).
    float3 outputRgb = bloom + dither;
    outputRgb += scene;
    outputRgb *= _Fade;

    float outputAlpha = center.a;
    // The clear route writes one and only applies without LIV_MR (ME2/ME3).
    #if defined(CLEAR_SCREEN_ALPHA) && !defined(LIV_MR)
    outputAlpha = 1.0;
    #endif

    return float4(outputRgb, outputAlpha);
}

#endif
    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            #pragma multi_compile_local _ LIV_MR
            #pragma multi_compile_local _ CLEAR_SCREEN_ALPHA
            #pragma vertex VertDefault
            #pragma fragment FragMainEffect
            ENDHLSL
        }
    }
}
