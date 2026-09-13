Shader "ChroMapper/Glowing"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // G1. The 1.42.2 source Properties block below is authoritative. Standard
    //     Unity KeywordEnum/Toggle attributes replace no property names or defaults.
    // G2 [a5ad0fe89169ad76,03903b03afec0941]: the base route returns the material
    //     _Color directly. _Color is not an instanced fragment property.
    // G3 [be31848f99a79f3e,dd1cfb62808fef87]: MainEffect white boost is
    //     saturate(color.rgb + color.a^2 * _BaseColorBoost -
    //     _BaseColorBoostThreshold), with alpha unchanged. It compiles out when
    //     MAIN_EFFECT_ENABLED is active; ChroMapper maps that global to POST_BLOOM.
    // G4 [e31bc067e9b5738a,c06f1f3a78ccef72]: bloom fog modulates start offset by
    //     color alpha and lerps fog scale from 1 to _FogScale by color alpha.
    //     Fog.hlsl samples the bloom prepass and lerps the full RGBA result.
    // G5 [76795e0bec45bc2d,6ff954e28b4d0c9d]: the vertex route outputs clip
    //     position, world position, and projected screen position. Instancing and
    //     stereo only select transforms, eye matrices, and the render-target slice.
    // G6. The recovered matrix contains no _CUTOUT_NORMAL, _NOISE_DITHERING,
    //     _ENABLE_COLOR_INSTANCING, or _WHITEBOOSTTYPE_ALWAYS binary. Their
    //     authoritative properties remain exposed but intentionally inert.
    // G7. ENABLE_BLOOM_FOG is normalized to ChroMapper's BLOOM_FOG global. No
    //     debug or speculative cutout/dither/Always route is retained.
    // G8. Stage binaries do not contain ShaderLab render state. The established
    //     opaque queue, back-face culling, LEqual ZTest, and ZWrite remain unchanged.
    Properties
    {
        [Toggle(_ENABLE_COLOR_INSTANCING)] _EnableColorInstancing ("Enable Color Instancing", Float) = 0
        _Color ("Color", Vector) = (1,0,0,0)

        [Space]
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1

        [Space]
        [KeywordEnum(None, Normal)] _CUTOUT ("Cutout Mode", float) = 0
        _Cutout ("Cutout", Range(0, 1)) = 0
        _CutoutTexScale ("Cutout Texture Scale", float) = 1
        _CutoutTexOffset ("Cutout Texture Offset", Vector) = (0,0,0,0)
        [HideInInspector] _CutoutTex ("Cutout Texture", 3D) = "white" {}

        [Space]
        [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", float) = 0

        [Space]
        [Toggle(_NOISE_DITHERING)] _NoiseDithering ("Noise Dithering", float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

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

            #pragma shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT
            // Global: the post-process bloom runs (ChroMapper drives it via
            // Shader.EnableKeyword; mirrors the game's MAIN_EFFECT_ENABLED gate).
            #pragma multi_compile _ POST_BLOOM

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"

            float4 _Color;
            float _FogStartOffset;
            float _FogScale;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos.xyz = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 albedo = _Color;

                #if defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)
                albedo.rgb = CalculateBloomComposition(
                    albedo.rgb, 1.0, albedo.a, 1.0,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif

                #if defined(BLOOM_FOG)
                float3 cameraPosition = GetStereoAwareCameraPosition();
                float3 cameraDelta = i.worldPos - cameraPosition;
                float distanceSq = dot(cameraDelta, cameraDelta);
                float fogStartOffset = albedo.a * _FogStartOffset;
                float fogScale = lerp(1.0, _FogScale, albedo.a);
                float fogFactor = CalculateCustomFogFactor(distanceSq, fogStartOffset, fogScale);
                albedo = ApplyBloomFogCalculatedFactor(albedo, i.screenPos, fogFactor);
                #endif

                return albedo;
            }
            ENDHLSL
        }
    }
}