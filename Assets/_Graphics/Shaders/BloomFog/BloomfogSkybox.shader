Shader "ChroMapper/BloomfogSkybox"
{
    // AUDIT FINDINGS (Beat Saber 1.42.2 / 1.44.3 comparison)
    // BS1 [406575e7, 8111e331]: BLOOM_FOG selects the bloom prepass sample;
    //     blue-noise dithering is applied after that branch for every route.
    // BS2 [2c016839, d8ea76ea]: noise coordinates include object translation;
    //     bloom UV coordinates remain unchanged.
    // BS3. OVERDRAW_VIEW is a debug route and is intentionally omitted.
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Geometry+100"
        }
        LOD 200
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_instancing

#ifndef CHROMAPPER_RECOVERED_BLOOMFOG_SKYBOX_PASS_INCLUDED
#define CHROMAPPER_RECOVERED_BLOOMFOG_SKYBOX_PASS_INCLUDED

#include "UnityCG.cginc"
#include "../ShaderLibrary/Common/PostProcess.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float4 bloomScreenPos : TEXCOORD0;
    float4 noiseScreenPos : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _BloomPrePassTexture;
sampler2D _GlobalBlueNoiseTex;
float2 _CustomFogTextureToScreenRatio;
float2 _GlobalBlueNoiseParams;
float _GlobalRandomValue;

#if defined(SHADER_API_D3D11) && defined(STEREO_INSTANCING_ON)
float2 _StereoCameraEyeOffsets;
#endif

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #if defined(UNITY_REVERSED_Z)
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    #else
    o.vertex = float4(v.vertex.xy, 1.0, 1.0);
    #endif

    float2 normalizedPosition =
        float2(v.vertex.x, v.vertex.y * _ProjectionParams.x) * 0.5 + 0.5;
    #if defined(SHADER_API_D3D11) && defined(STEREO_INSTANCING_ON)
    normalizedPosition.x += _StereoCameraEyeOffsets[unity_StereoEyeIndex];
    #endif
    o.bloomScreenPos = float4(
        (normalizedPosition - 0.5) * _CustomFogTextureToScreenRatio + 0.5,
        0.0,
        1.0);
    o.noiseScreenPos = BuildNoiseScreenPosition(
        float4(normalizedPosition, 0.0, 1.0), o.vertex,
        _GlobalBlueNoiseParams, _GlobalRandomValue,
        unity_ObjectToWorld._m03_m13);
    return o;
}

half4 frag(v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #if defined(BLOOM_FOG)
    // Beat Saber's Custom/BloomSkyboxQuad shows the bloom prepass
    // texture with a blue-noise dither offset, nothing else.
    half4 col = tex2D(
        _BloomPrePassTexture,
        i.bloomScreenPos.xy / i.bloomScreenPos.ww);
    #else
    half4 col = half4(0.1, 0.1, 0.1, 0.0);
    #endif
    col = ApplyNoiseDither(col, i.noiseScreenPos, _GlobalBlueNoiseTex);
    col.a = 0;
    return col;
}

#endif
            ENDHLSL
        }
    }
}
