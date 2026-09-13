Shader "ChroMapper/Post Process/Bloom"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"
    #include "../ShaderLibrary/Families/BloomShared.hlsl"

    struct VaryingsDefault
    {
        float4 vertex : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    VaryingsDefault VertDefault(appdata_img v)
    {
        VaryingsDefault o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = v.texcoord;
        return o;
    }

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    TEXTURE2D(_BloomTex);
    SAMPLER(sampler_BloomTex);
    TEXTURE2D(_GlobalIntensityTex);
    SAMPLER(sampler_GlobalIntensityTex);
    float4 _BloomTexelSize;
    float _SampleScale;
    float4 _BloomParams;
    float4 _CombineParams;

    float4 FragDownsample13Alpha(VaryingsDefault i) : SV_Target
    {
        return BloomAlphaGate(BloomDownsample13Classic(
                                  TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _BloomTexelSize.xy),
                              _BloomParams.z);
    }

    float4 FragDownsample4Alpha(VaryingsDefault i) : SV_Target
    {
        return BloomAlphaGate(BloomDownsample4(
                                  TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, abs(_BloomTexelSize.xy)),
                              _BloomParams.z);
    }

    float4 FragDownsample13(VaryingsDefault i) : SV_Target
    {
        return BloomDownsample13Classic(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _BloomTexelSize.xy);
    }

    float4 FragDownsample4(VaryingsDefault i) : SV_Target
    {
        // UV clamping remains a sampler-addressing compatibility choice.
        return BloomDownsample4(
            TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, abs(_BloomTexelSize.xy));
    }

    float4 FragDownsample4Gamma(VaryingsDefault i) : SV_Target
    {
        return BloomApplyGamma(FragDownsample4(i));
    }

    float4 FragUpsampleTent(VaryingsDefault i) : SV_Target
    {
        float4 upsampled = BloomUpsampleTent(TEXTURE2D_PARAM(_MainTex, sampler_MainTex),
                                             i.texcoord, _BloomTexelSize.xy, _SampleScale);
        return BloomWeightedCombine(SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord),
                                    upsampled, _CombineParams.x, _CombineParams.y);
    }

    float4 FragUpsampleBox(VaryingsDefault i) : SV_Target
    {
        float4 upsampled = BloomUpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex),
                                            i.texcoord, _BloomTexelSize.xy, _SampleScale);
        return BloomWeightedCombine(SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord),
                                    upsampled, _CombineParams.x, _CombineParams.y);
    }

    float4 FragUpsampleTentGamma(VaryingsDefault i) : SV_Target
    {
        return BloomApplyGamma(FragUpsampleTent(i));
    }

    float4 FragUpsampleBoxGamma(VaryingsDefault i) : SV_Target
    {
        return BloomApplyGamma(FragUpsampleBox(i));
    }

    float4 FragDirectCombine(VaryingsDefault i) : SV_Target
    {
        return SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoord) * _CombineParams.x +
            SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord) * _CombineParams.y;
    }

    float4 FragDirectCombineGamma(VaryingsDefault i) : SV_Target
    {
        return BloomApplyGamma(FragDirectCombine(i));
    }

    float4 FragUpsampleReinhard(VaryingsDefault i) : SV_Target
    {
        float4 col = FragUpsampleTent(i);
        col.rgb = col.rgb * (col.rgb * 0.25 + 1.0) / (col.rgb + 1.0);
        return col;
    }

    float4 FragUpsampleAces(VaryingsDefault i) : SV_Target
    {
        return ApplyAcesTonemapping(FragUpsampleTent(i));
    }

    float4 FragUpsampleAutoExposureAces(VaryingsDefault i) : SV_Target
    {
        float4 combined = FragUpsampleTent(i);
        float3 intensity = SAMPLE_TEXTURE2D(_GlobalIntensityTex, sampler_GlobalIntensityTex,
                                            float2(0.5, 0.5)).rgb;
        return BloomApplyKneeAndAces(combined, intensity, _BloomParams.x, _BloomParams.w);
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        // The renderer selects these passes by index.
        // 0: 13-tap + alpha gate
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample13Alpha
            ENDHLSL
        }
        // 1: 4-tap + alpha gate
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample4Alpha
            ENDHLSL
        }
        // 2: 13-tap
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample13
            ENDHLSL
        }
        // 3: 4-tap
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample4
            ENDHLSL
        }
        // 4: 4-tap + gamma
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDownsample4Gamma
            ENDHLSL
        }
        // 5: tent + combine
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleTent
            ENDHLSL
        }
        // 6: box + combine
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleBox
            ENDHLSL
        }
        // 7: tent + combine + gamma
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleTentGamma
            ENDHLSL
        }
        // 8: box + combine + gamma
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleBoxGamma
            ENDHLSL
        }
        // 9: direct weighted combine
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDirectCombine
            ENDHLSL
        }
        // 10: direct combine + gamma
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragDirectCombineGamma
            ENDHLSL
        }
        // 11: tent + combine + Reinhard
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleReinhard
            ENDHLSL
        }
        // 12: tent + combine + ACES
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleAces
            ENDHLSL
        }
        // 13: tent + combine + auto-exposure + ACES
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment FragUpsampleAutoExposureAces
            ENDHLSL
        }
    }
}
