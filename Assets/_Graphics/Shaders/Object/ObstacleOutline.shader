Shader "ChroMapper/Object/Obstacle Outline"
{
    // AUDIT FINDINGS
    // 1. Source comparison: this editor adapter keeps the source outline's
    //    face-normal UV frame and bloom composition, but does not replace the
    //    game's ObstacleCore or add unowned selection animation.
    // 2. Cube contract: the editor supplies Unity's built-in cube mesh with 24
    //    face-local vertices/normals and 0..1 UVs. Face dimensions are selected
    //    by normal: +/-X -> ZY, +/-Y -> XZ, and +/-Z -> XY.
    // 3. Adapter-only properties: _Color, _WorldScale, _SizeParams, _Cutout,
    //    and _CutoutTexOffset are instanced inputs written by editor MPBs.
    //    Fog controls remain because the owned CM_PREVIEW_MODE+BLOOM_FOG route
    //    passes them to the shared bloom-fog calculation.
    // 4. OVERDRAW_VIEW is intentionally omitted; no editor owner or render
    //    path requires that source debug variant.
    Properties
    {
        _FogStartOffset ("Fog Start Offset", Float) = 1
        _FogScale ("Fog Scale", Float) = 1
        _FogHeightScale ("Fog Height Scale", Float) = 1
        _FogHeightOffset ("Fog Height Offset", Float) = 0

        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0

        [Space]
        [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("Bloom Type", Float) = 1

        [Space]
        [Toggle(CUTOUT)] _EnableCutout ("Enable Cutout", Float) = 0
        _CutoutTexScale ("Cutout Texture Scale", Float) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor ("Foreground Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor ("Background Factor", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactorA ("Foreground Alpha Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactorA ("Background Alpha Factor", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        [Toggle] _ZWrite ("Z Write", Float) = 1

        [Space]
        // ChroMapper compatibility. ObstacleContainer and ObjectAnimator write these through an MPB.
        _Color ("Color", Color) = (0.5, 0, 0, 1)
        _WorldScale ("World Scale", Vector) = (1, 1, 1, 1)
        _Cutout ("Cutout", Range(0, 1)) = 0
        _CutoutTexOffset ("Cutout Texture Offset", Vector) = (0, 0, 0, 0)
        _SizeParams ("Size Params", Vector) = (0.5, 0.5, 0.5, 0.025)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+3"
            "RenderType" = "Opaque"
        }

        Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment CUTOUT
            #pragma shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS
            #pragma multi_compile _ POST_BLOOM

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ CM_PREVIEW_MODE

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "../ShaderLibrary/Common/Bloom.hlsl"
            #include "../ShaderLibrary/Cutout.hlsl"

            sampler3D _CutoutTex;
            float _CutoutTexScale;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPosCustom(o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
                float2 faceScale;
                if (abs(i.normal.x) > 0.5)
                    faceScale = worldScale.zy;
                else if (abs(i.normal.y) > 0.5)
                    faceScale = worldScale.xz;
                else
                    faceScale = worldScale.xy;

                // Preserve the existing cube-mesh frame construction while exposing the HD contract.
                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);
                // _SizeParams.w is the source half edge width; the UV frame
                // needs the full physical edge width on this cube contract.
                float frameWidth = max(sizeParams.w, 0.0001);
                float2 distanceFromEdge = 0.5 - abs(0.5 - i.uv);
                clip(frameWidth - min(distanceFromEdge.x * faceScale.x,
                                      distanceFromEdge.y * faceScale.y));

                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                #if defined(CUTOUT)
                float3 objectOrigin = unity_ObjectToWorld._m03_m13_m23;
                float3 cutoutPosition = CalculateObjectSpaceCutoutPosition(
                    i.worldPos, objectOrigin, cutoutTexOffset.xyz, _CutoutTexScale);
                float cutoutNoise = tex3D(_CutoutTex, cutoutPosition).a;
                ApplyCutoutNoise(cutoutNoise, cutout);
                #endif

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                #if !defined(CM_PREVIEW_MODE)
                color.a = 0;
                #else
                // ParametricBoxFrameHD stores bloom intensity in twice the color alpha.
                color.a = max(color.a, 0) * 2;
                #endif

                #if defined(CM_PREVIEW_MODE) && defined(BLOOM_FOG)
                color = ApplyBloomHeightFog(color, i.screenPos, i.worldPos, _FogStartOffset, _FogScale,
                                            _FogHeightOffset, _FogHeightScale);
                #endif

                #if defined(_WHITEBOOSTTYPE_ALWAYS) || (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM))
                // ParametricBoxFrameHD keeps base RGB unpremultiplied and uses
                // twice the color alpha as its white-boost input.
                color.rgb = CalculateBloomComposition(color.rgb, 1, color.a, 1,
                                                      _BaseColorBoost,
                                                      _BaseColorBoostThreshold);
                #elif defined(_WHITEBOOSTTYPE_MAINEFFECT)
                // POST_BLOOM on mirrors MAIN_EFFECT_ENABLED: the white boost is
                // omitted and the unpremultiplied frame color is preserved.
                #endif

                return color;
            }
            ENDHLSL
        }
    }
}