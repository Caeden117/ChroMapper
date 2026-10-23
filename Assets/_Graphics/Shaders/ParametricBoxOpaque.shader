// Opaque parametric light volume with instanced color and fog-shaped alpha.
Shader "ChroMapper/Parametric Box Opaque"
{
    Properties
    {
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200
        Cull Back
        ZTest LEqual
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment HEIGHT_FOG
            // Global: enabled by the bloom-fog renderer during its pass.
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile _ POST_BLOOM
            #pragma multi_compile_fragment _ OVERDRAW_VIEW

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Common/Overdraw.hlsl"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"
            #include "ShaderLibrary/Families/ParametricShared.hlsl"

            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float _GlobalRandomValue;
            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 noiseScreenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                o.noiseScreenPos = BuildNoiseScreenPosition(
                    o.screenPos, o.vertex, _GlobalBlueNoiseParams,
                    _GlobalRandomValue, unity_ObjectToWorld._m03_m13);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                #if defined(OVERDRAW_VIEW)
                return CalculateParametricOverdrawColor();
                #endif

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float alpha = color.a * color.a;
                float heightFactor = 1.0;

                // Height transmission shapes alpha before either distance-fog evaluation.
                #if defined(HEIGHT_FOG)
                heightFactor = CalculateParametricHeightRamp(
                    i.worldPos.y, _FogHeightScale, _FogHeightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);
                #endif

                float3 cameraPosition = GetStereoAwareCameraPosition();
                float fogTransmission = 1.0;

                // Alpha attenuation and fog blending intentionally use different divisors.
                #if defined(BLOOM_FOG)
                alpha *= heightFactor * CalculateParametricDistanceTransmission(
                    i.worldPos, cameraPosition, _FogStartOffset, _FogScale, color.a,
                    _CustomFogOffset, _CustomFogAttenuation);
                fogTransmission = CalculateParametricDistanceTransmission(
                    i.worldPos, cameraPosition, _FogStartOffset, _FogScale, alpha,
                    _CustomFogOffset, _CustomFogAttenuation);
                #else
                alpha *= heightFactor;
                #endif
                float fogBlend = 1.0 - heightFactor * fogTransmission;

                float3 rgb = color.rgb * alpha;
                // Post-bloom composition suppresses the local white-boost term.
                #if !defined(POST_BLOOM)
                rgb = CalculateBloomComposition(color.rgb, alpha, alpha, 1,
                                                _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                // Dither is unconditional and precedes the final fog-target composition.
                rgb = ApplyNoiseDither(
                    float4(rgb, alpha), i.noiseScreenPos, _GlobalBlueNoiseTex).rgb;

                float3 fogTarget = 0.1;
                #if defined(BLOOM_FOG)
                fogTarget = SampleBloomPrePass(i.screenPos).rgb;
                #endif
                rgb = rgb + rgb + fogBlend * (fogTarget - rgb);

                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
