// Replacement for the Beat Saber game shader Custom/ParametricBoxFakeGlow.
Shader "ChroMapper/Parametric Box Fake Glow"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // PFG1. The 1.44.3 Custom/ParametricBoxFakeGlow Properties block is
    //       authoritative. Color, size, cutout, clipping, and noise
    //       inputs are runtime/instanced uniforms and remain unexposed.
    // PFG2 [139018e71a01a3e2]: POSITION, UV0, and NORMAL are the only mesh
    //       inputs. Face-relative deformation starts at -sign(position) and keeps
    //       _SizeParams.w as a constant border width. UV0 has no _MainTex_ST.
    // PFG3 [1afc20561ed2144b]: CUTOUT scales local XY by 1 - _Cutout squared
    //       before the object transform. This shader does not use _AnimationSpawned.
    // PFG4 [139018e71a01a3e2]: angle fade is saturate(abs(dot(normalized
    //       camera-to-vertex, normalized world normal) * _AngleDisappearParam)).
    // PFG5 [e329b30d3474ad13,d2a5af8334ac0b36]: texture alpha is squared.
    //       Alpha then uses the shared cubic height ramp, angle fade, and color
    //       alpha. BLOOM_FOG also multiplies the shared distance transmission.
    // PFG6 [01fa7ac4ac5f6546]: WORLDSPACE_NOISE_CUTOUT samples 3D noise alpha
    //       at (worldPos - objectOrigin + _CutoutTexOffset) * scale and applies
    //       the shared 1.1 * cutout - 0.1 threshold. CUTOUT alone is vertex-only.
    // PFG7 [2dffd03d72718568]: CLIPPING is a world-space plane discard. The
    //       ChroMapper runtime uses its established global _ClippingPlane form.
    // PFG8 [acbb090e6240a31d,0020a90d28235a82]: output is premultiplied RGBA.
    //       Always white boost remains active; MainEffect boost is disabled by
    //       MAIN_EFFECT_ENABLED, mapped to ChroMapper's POST_BLOOM global.
    // PFG9. ENABLE_ material keywords normalize to CUTOUT, CLIPPING, and
    //       MAIN_EFFECT_WHITE_BOOST. ENABLE_BLOOM_FOG maps to BLOOM_FOG.
    //       OVERDRAW_VIEW remains intentionally omitted.
    // PFG10. Stage binaries do not prove ShaderLab state. The transparent,
    //        additive, double-sided, LEqual, and ZWrite Off replacement state
    //        is retained; only the four authoritative blend factors are exposed.
    Properties
    {
        [Space] _MainTex ("Main Texture", 2D) = "white" {}

        [Space] _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        [ShowIfAny(HEIGHT_FOG)] _FogHeightScale ("Fog Height Scale", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightOffset ("Fog Height Offset", float) = 0

        [Space] _AngleDisappearParam ("Angle disappear param", float) = 1
        [Space] [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", Float) = 1
        [Toggle(CUTOUT)] _EnableCutout ("Enable Vertex Cutout", float) = 0
        [ToggleShowIfAny(WORLDSPACE_NOISE_CUTOUT, CUTOUT)] _WorldspaceNoiseCutout ("Worldspace Noise Cutout", float) = 0
        [ShowIfAny(2, CUTOUT, WORLDSPACE_NOISE_CUTOUT)] _CutoutTexScale ("Cutout Noise Scale", float) = 1
        [Toggle(CLIPPING)] _EnableClipping ("Enable Clipping", float) = 0

        [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp Add
        Cull Off
        ZTest LEqual
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            // Global: enabled by the bloom-fog renderer during its pass.
            #pragma multi_compile _ BLOOM_FOG
            #pragma shader_feature_local HEIGHT_FOG
            #pragma shader_feature_local MAIN_EFFECT_WHITE_BOOST
            #pragma shader_feature_local _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS
            #pragma shader_feature_local CUTOUT
            #pragma shader_feature_local_fragment WORLDSPACE_NOISE_CUTOUT
            #pragma shader_feature_local_fragment CLIPPING
            // Global: the post-process bloom runs (mirrors the game's MAIN_EFFECT_ENABLED gate).
            #pragma multi_compile _ POST_BLOOM

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Cutout.hlsl"
            #include "ShaderLibrary/Families/ParametricShared.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            float _AngleDisappearParam;
            sampler3D _CutoutTex;
            float _CutoutTexScale;
            float4 _ClippingPlane;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                // DXBC 139018e7: retain each vertex's face and scale its offset
                // from that face to keep the border width constant.
                float3 faceSide = sign(i.vertex.xyz);
                i.vertex.xyz = faceSide +
                    (i.vertex.xyz - faceSide) * (2.0 * sizeParams.w / sizeParams.xyz);

                #if defined(CUTOUT)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                i.vertex.xy *= 1.0 - cutout * cutout;
                #endif

                o.vertex = UnityObjectToClipPos(i.vertex);

                // DXBC 139018e7: raw UV passes through; the game samples it
                // untransformed (no ST application in vertex or fragment).
                o.uv.xy = i.uv.xy;
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;

                float3 viewDirection = normalize(o.worldPos - GetParametricCameraPosition());
                float3 worldNormal = UnityObjectToWorldNormal(i.normal);
                // DXBC 139018e7: angle factor is multiplied before interpolation;
                // the fragment route only multiplies by instanced color alpha.
                o.uv.z = min(abs(dot(viewDirection, worldNormal) * _AngleDisappearParam), 1.0);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                // DXBC e329b30d/acbb090e/542c8823: alpha = texAlpha² * saturate(
                // heightRamp * [fogInverse] * angleFade * color.a). The height
                // ramp is applied unconditionally from the shared fog globals;
                // HEIGHT_FOG only switches in the per-material scale and offset.
                float alpha = tex2D(_MainTex, i.uv.xy).a;
                alpha *= alpha;

                float heightScale = 1.0;
                float heightOffset = 0.0;
                #if defined(HEIGHT_FOG)
                heightScale = _FogHeightScale;
                heightOffset = _FogHeightOffset;
                #endif
                float heightRamp = CalculateParametricHeightRamp(
                    i.worldPos.y, heightScale, heightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);

                #if defined(WORLDSPACE_NOISE_CUTOUT)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float3 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset).xyz;
                float3 objectOrigin = unity_ObjectToWorld._m03_m13_m23;
                float3 cutoutPosition = CalculateObjectSpaceCutoutPosition(
                    i.worldPos, objectOrigin, cutoutTexOffset, _CutoutTexScale);
                ApplyCutoutNoise(
                    tex3D(_CutoutTex, cutoutPosition).w, cutout);
                #endif
                #if defined(CLIPPING)
                clip(dot(float4(i.worldPos, 1.0), _ClippingPlane));
                #endif

                #if defined(BLOOM_FOG)
                // DXBC d2a5af83/acbb090e: bloom fog attenuates alpha through the
                // distance transmission; it does not sample the bloom pre-pass.
                float3 cameraPosition = GetParametricCameraPosition();
                float fogInverse = CalculateParametricDistanceTransmission(
                    i.worldPos, cameraPosition, _FogStartOffset, _FogScale, 1.0,
                    _CustomFogOffset, _CustomFogAttenuation);
                alpha *= saturate(heightRamp * fogInverse * i.uv.z * color.a);
                #else
                alpha *= saturate(heightRamp * i.uv.z * color.a);
                #endif

                half4 result = half4(color.rgb * alpha, alpha);
                // DXBC acbb090e (Always) / 0020a90d (MainEffect): the white boost
                // adds alpha² * _BaseColorBoost - _BaseColorBoostThreshold to the
                // premultiplied color. The game's ENABLE_MAIN_EFFECT_WHITE_BOOST
                // is a pipeline global; ChroMapper drops the ENABLE_ prefix and
                // preserves it as a material keyword without exposing a property.
                // MainEffect type compiles the boost out while POST_BLOOM runs.
                #if defined(MAIN_EFFECT_WHITE_BOOST) && \
                    (defined(_WHITEBOOSTTYPE_ALWAYS) || \
                     (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)))
                result.rgb = CalculateBloomComposition(
                    color.rgb, alpha, alpha, 1,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                return result;
            }
            ENDHLSL
        }
    }
}