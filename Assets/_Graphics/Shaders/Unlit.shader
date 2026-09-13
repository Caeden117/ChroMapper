Shader "ChroMapper/Unlit"
{
    Properties
    {

        [Space(10)]
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", Float) = 0

        [Header(Fog Settings)] [Space]
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", float) = 1
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", float) = 1
        [Space]
        [ToggleShowIfAny(HEIGHT_FOG, FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0
        [ShowIfAny(2, FOG, HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1

        [Header(Settings)] [Space]
        [Toggle(ALPHA_CUTOUT)] _AlphaCutout ("Alpha Cutout", float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Cull [_CullMode]
            ZTest [_ZTest]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment ALPHA_CUTOUT
            #pragma shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS
            // Global: the post-process bloom runs (mirrors the game's MAIN_EFFECT_ENABLED gate).
            #pragma multi_compile _ POST_BLOOM
            #pragma shader_feature_local_fragment HEIGHT_FOG

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;

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
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.uv.xy = i.uv.xy;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                half4 albedo = color * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                #if defined(ALPHA_CUTOUT)
                if (albedo.a == 0) discard;
                #endif

                // The game's Unlit family has no white boost. The dispatcher keeps
                // its no-bloom alpha contract and the Deferred/Mixed adapters.
                #if defined(_WHITEBOOSTTYPE_ALWAYS) || (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM))
                albedo.rgb = CalculateBloomComposition(
                    albedo.rgb, albedo.a, albedo.a, 1,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #elif defined(_WHITEBOOSTTYPE_MAINEFFECT)
                albedo = CalculateBloomPostComposition(albedo.rgb, albedo.a, 1.0);
                #else
                if (1 > 0.5)
                {
                    albedo.rgb *= albedo.a;
                    albedo.a = 0;
                }
                #endif

                albedo = ApplyAcesTonemapping(albedo);

                #if defined(BLOOM_FOG)
                #if defined(HEIGHT_FOG)
                albedo = ApplyBloomHeightFog(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale,
                                             _FogHeightOffset, _FogHeightScale);
                #else
                albedo = ApplyBloomFog(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}