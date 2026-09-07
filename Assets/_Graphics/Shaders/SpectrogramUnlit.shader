// Replacement for the Beat Saber game shader Custom/UnlitSpectrogram.
Shader "ChroMapper/Spectrogram Unlit"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // U1. The 1.44.3 Custom/UnlitSpectrogram Properties block is authoritative;
    //     _BlendMode* are established importer aliases for _Blend*Factor.
    // U2 [131d5989fe263d58]: UV.x selects uint(max(uv.x * 63, 0)). Visibility
    //     is step(uv.y, (sample + 0.05) * _SpectrogramScale). RGB is _Color.rgb
    //     and alpha is visibility * _Color.a.
    // U3 [80969e1d39b26e44]: ENABLE_BLOOM_FOG maps to ChroMapper's BLOOM_FOG
    //     global and lerps the complete RGBA value toward the projected bloom
    //     prepass sample. No white boost, tonemapping, or dithering route exists.
    // U4 [25a7770007c1a811,65e0d97fd4c2560c]: POSITION and UV0 are the only
    //     mesh inputs. Instancing selects transforms; stereo selects eye matrices
    //     and render-target slices. Diagnostic OVERDRAW_VIEW remains omitted.
    // U5. Stage binaries do not contain ShaderLab render-state metadata. The
    //     established transparent blend, Cull Off, LEqual, and ZWrite Off remain.
    Properties
    {
        _Color ("Color", Vector) = (1,1,1,1)
        _SpectrogramScale ("Spectrogram Scale", float) = 0.5

        [Space]
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1

        [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src Factor", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst Factor", float) = 10
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", float) = 10
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "DisableBatching"="True"
        }

        LOD 200
        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        Cull Off
        ZTest LEqual
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Families/SpectrogramShared.hlsl"

            float _SpectrogramData[64];
            float _SpectrogramScale;

            float _FogStartOffset;
            float _FogScale;

            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.uv.xy = i.uv.xy;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                uint index = CalculateSpectrogramIndex(i.uv.x);
                float visible = step(
                    i.uv.y, (_SpectrogramData[index] + 0.05) * _SpectrogramScale);
                float4 albedo = float4(_Color.rgb, _Color.a * visible);

                #if defined(BLOOM_FOG)
                albedo = ApplyBloomFog(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}