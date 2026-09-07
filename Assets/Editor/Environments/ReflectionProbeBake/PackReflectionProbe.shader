// Packs the six bake-ID face renders of a BakedReflectionProbe into the two
// packed cubemap channels that Reflection.hlsl decodes at runtime.
//
// The decode contract (Reflection.hlsl:DecodeReflectionProbePair) is:
//   probe1.rgb = bake IDs A / B / C, probe2.rgb = bake IDs D / E / F,
//   decoded  = c * bakeId.rgb                          for c <= 0.5
//   decoded  = 0.5 * bakeId.rgb + ((c - 0.5) * bakeId.w)^2   for c > 0.5
//   result   = saturate(decoded * 2.0) * reflectionProbeIntensity
//
// The original Hidden/BakedLightTexturePacking shader copies the red channel
// from each source texture into the packed RGB channels. Values from 0 through
// 0.5 are the colored contribution. Values
// above 0.5 are the highlight contribution. The baking bloom profile creates
// that high band before this shader packs the channels.
Shader "Hidden/ChroMapper/ReflectionProbeBakePack"
{
    Properties
    {
        _ProbeRawTex ("Probe Raw Texture", 2D) = "black" {}
        _ProbeBloomTex ("Probe Bloom Texture", 2D) = "black" {}
        _ProbeSourceA ("Probe Source A", 2D) = "black" {}
        _ProbeSourceB ("Probe Source B", 2D) = "black" {}
        _ProbeSourceC ("Probe Source C", 2D) = "black" {}
        _ProbeBlend ("Probe Blend", Float) = 0.2
        _ProbeSourceTexelOffset ("Probe Source Texel Offset", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: blend the raw and bloomed face renders of one bake ID.
        // When the packed cubemap is smaller than the raw render (the probe's
        // ResolutionBeforeDownsample >> DownsampleByHalfCount), the source
        // sampler averages a 2x2 source texel box so this pass also performs
        // the downsample.
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBlend

            #include "UnityCG.cginc"

            sampler2D _ProbeRawTex;
            sampler2D _ProbeBloomTex;
            float _ProbeBlend;
            float4 _ProbeSourceTexelOffset;

            float4 SampleProbeSource(sampler2D source, float2 uv)
            {
                float4 color = tex2D(source, uv + _ProbeSourceTexelOffset.xy);
                color += tex2D(source, uv + float2(_ProbeSourceTexelOffset.x, -_ProbeSourceTexelOffset.y));
                color += tex2D(source, uv + float2(-_ProbeSourceTexelOffset.x, _ProbeSourceTexelOffset.y));
                color += tex2D(source, uv - _ProbeSourceTexelOffset.xy);
                return color * 0.25;
            }

            float4 FragBlend(v2f_img i) : SV_Target
            {
                float4 raw = SampleProbeSource(_ProbeRawTex, i.uv);
                float3 bloom = SampleProbeSource(_ProbeBloomTex, i.uv).rgb;
                float3 scene = saturate(raw.rgb + raw.a * raw.a);
                return float4(scene + bloom * _ProbeBlend, 1.0);
            }
            ENDHLSL
        }

        // Pass 1: encode three blended bake-ID faces into one packed RGB face
        // (probe 1 = bake IDs A-C, probe 2 = bake IDs D-F). Runs at the packed
        // cubemap resolution, sampling each source one to one.
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragPack

            #include "UnityCG.cginc"

            sampler2D _ProbeSourceA;
            sampler2D _ProbeSourceB;
            sampler2D _ProbeSourceC;
            float4 FragPack(v2f_img i) : SV_Target
            {
                float a = tex2D(_ProbeSourceA, i.uv).r;
                float b = tex2D(_ProbeSourceB, i.uv).r;
                float c = tex2D(_ProbeSourceC, i.uv).r;
                return float4(
                    a,
                    b,
                    c,
                    0.0);
            }
            ENDHLSL
        }
    }
}
