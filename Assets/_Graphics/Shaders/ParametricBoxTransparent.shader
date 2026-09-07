// Replacement for the Beat Saber game shader Custom/TransparentNeonLight.
Shader "ChroMapper/Parametric Box Transparent"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // PBT1. The 1.42.2 Custom/TransparentNeonLight Properties block is
    //       authoritative. _Color and _AlphaWidth remain instanced runtime
    //       values; shared fog, time, noise, bloom, and probe inputs are globals.
    // PBT2. POSITION drives all routes. REFLECTION_PROBE also reads NORMAL.
    //       The vertex selects _AlphaWidth zw/x y by position.y > 0.5, scales
    //       local XZ by that width, and interpolates the selected alpha factor.
    // PBT3 [10c6243ca42145bf,fb0a3537aa32426e]: source alpha is selected width
    //       alpha cubed times _Color.a. WORLD_NOISE multiplies sampled 3D alpha,
    //       intensity, optional warp, scrolling time, and optional world fade.
    //       The result is then squared.
    // PBT4. HEIGHT_FOG multiplies the squared alpha by the shared cubic height
    //       ramp. BLOOM_FOG additionally multiplies distance transmission using
    //       the pre-square source alpha as its divisor.
    // PBT5 [866b14486d5d6356,032dfb9c99253066]: reflection uses the normalized
    //       world normal and view ray, roughness-adjusted mip, and both packed
    //       probes. It saturates after decode and again after reflection intensity.
    // PBT6. Reflection is added independently of source alpha, scaled by squared
    //       fog transmission and _GlassOpacity. Output alpha retains source fog.
    // PBT7. MAIN_EFFECT_ENABLED disables source white boost; ChroMapper maps it
    //       to POST_BLOOM and reuses CalculateBloomComposition.
    // PBT8. No 1.44.3 binary contains SPECULAR, NORMAL_MAP, ENABLE_RIM_DIM, or
    //       INVERT_RIM_DIM. Their authoritative controls remain exposed but inert.
    //       OVERDRAW_VIEW remains intentionally omitted.
    // PBT9. Stage binaries cannot prove ShaderLab state. Established transparent
    //       blend/cull/stencil controls, LEqual, and ZWrite Off remain.
    Properties
    {
        _FogStartOffset ("Fog Start Offset", float) = 1
        _FogScale ("Fog Scale", float) = 1
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightScale ("Height Fog Scale", float) = 1
        [ShowIfAny(HEIGHT_FOG)] _FogHeightOffset ("Height Fog Offset", float) = 0

        [Space(12)] [Toggle(WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScale ("World Noise Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityOffset ("World Intensity Offset", float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityScale ("World Intenstity Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0,0,0,1)
        [ToggleShowIfAny(WORLD_NOISE_WARP, WORLD_NOISE)] _WorldNoiseDistort ("Noise Field Warp", float) = 0
        [ShowIfAny(WORLD_NOISE_WARP)] _NoiseWarpZoomStrength ("Noise Warp Zoom Strength", float) = 0
        [ShowIfAny(WORLD_NOISE_WARP)] _NoiseWarpSkewStrength ("Noise Warp Skew Strength", float) = 0

        [Space(12)] [Toggle(WORLD_SPACE_FADE)] _EnableWorldSpaceFade ("Enable World Space Fade", float) = 0
        [ShowIfAny(WORLD_SPACE_FADE)] _WorldSpaceFadePos ("World Space Fade Position", float) = 0
        [ShowIfAny(WORLD_SPACE_FADE)] _WorldSpaceFadeSlope ("World Space Fade Slope", float) = 1

        [Space(12)] _EnableSpecular ("Enable Specular", float) = 1
        [ShowIfAny(SPECULAR)] _SpecularIntensity ("Specular Intensity", float) = 1
        [ShowIfAny(SPECULAR)] _SpecularHardness ("Specular Hardness", float) = 64
        [Space(12)] _EnableNormalMap ("Enable Normal Map", float) = 0
        [ShowIfAny(NORMAL_MAP)] _NormalTex ("Normal Texture", 2D) = "bump" {}
        [ShowIfAny(NORMAL_MAP)] _NormalScale ("Normal Scale", float) = 1

        [Header(Probe Reflection)] [Space(8)] [Toggle(REFLECTION_PROBE)] _EnableReflectionProbe ("Enable Reflection Probe", float) = 0
        [ShowIfAny(REFLECTION_PROBE)] _Smoothness ("Smoothness", Range(0, 1)) = 1
        [ShowIfAny(REFLECTION_PROBE)] _ReflectionIntensity ("Probe Intensity", float) = 1
        [ShowIfAny(REFLECTION_PROBE)] _GlassOpacity ("Glass Opacity", float) = 1
        [Space(8)] _EnableRimDim ("Enable Rim Dim", float) = 0
        [ShowIfAny(ENABLE_RIM_DIM)] _RimScale ("Rim Scale", float) = 1
        [ShowIfAny(ENABLE_RIM_DIM)] _RimOffset ("Rim Offset", float) = 1
        [ShowIfAny(ENABLE_RIM_DIM)] _RimDistanceOffset ("Rim Camera Distance Offset", float) = 2
        [ShowIfAny(ENABLE_RIM_DIM)] _RimDistanceScale ("Rim Camera Distance Scale", float) = 0.3
        _InvertRimDim ("Invert Rim Dim", float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", float) = 1
        [Space] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("CullMode", float) = 0
        [Space]
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Space] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest LEqual
        ZWrite Off

        Stencil
        {
            Ref [_StencilRefValue]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment WORLD_NOISE
            #pragma shader_feature_local_fragment WORLD_SPACE_FADE
            #pragma shader_feature_local_fragment WORLD_NOISE_WARP
            #pragma shader_feature_local REFLECTION_PROBE
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile _ POST_BLOOM

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Families/ParametricShared.hlsl"
            #include "ShaderLibrary/Common/Reflection.hlsl"

            sampler3D _CutoutTex;
            float4 _TimeHelperOffset;
            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityOffset;
            float _WorldNoiseIntensityScale;
            float3 _WorldNoiseScrolling;
            float _WorldSpaceFadePos;
            float _WorldSpaceFadeSlope;
            float _NoiseWarpZoomStrength;
            float _NoiseWarpSkewStrength;
            float _Smoothness;
            float _ReflectionIntensity;
            float _GlassOpacity;
            samplerCUBE _ReflectionProbeTexture1;
            samplerCUBE _ReflectionProbeTexture2;
            float4 _LightProbeLightBakeIdA;
            float4 _LightProbeLightBakeIdB;
            float4 _LightProbeLightBakeIdC;
            float4 _LightProbeLightBakeIdD;
            float4 _LightProbeLightBakeIdE;
            float4 _LightProbeLightBakeIdF;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlphaWidth)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                #if defined(REFLECTION_PROBE)
                float3 normal : NORMAL;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float alphaFactor : TEXCOORD1;
                #if defined(REFLECTION_PROBE)
                float3 worldNormal : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);
                // 0e524517: all eight vertex binaries use y > 0.5, lerp(z,w), and xz scaling.
                float top = i.vertex.y > 0.5 ? 1.0 : 0.0;
                float width = lerp(alphaWidth.z, alphaWidth.w, top);
                o.alphaFactor = lerp(alphaWidth.x, alphaWidth.y, top);
                i.vertex.xz *= width;

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                #if defined(REFLECTION_PROBE)
                #if defined(SHADER_API_D3D11)
                // The original D3D11 vertex transports the model normal without normalization.
                o.worldNormal = mul((float3x3)unity_ObjectToWorld, i.normal);
                #else
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                #endif
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                // f68fe436: alphaFactor^3 * instanced color alpha is the common route.
                float alpha = i.alphaFactor * i.alphaFactor * i.alphaFactor * color.a;

                // 10c6243c/fb0a3537: noise uses scrolling time, 3D texture alpha, then offset/scale.
                #if defined(WORLD_NOISE)
                float noise = SampleParametricWorldNoise(
                    i.worldPos, _CutoutTex, _WorldNoiseScrolling,
                    _Time.x + _TimeHelperOffset.x, _WorldNoiseScale,
                    _WorldNoiseIntensityOffset, _WorldNoiseIntensityScale,
                    _WorldSpaceFadePos, _WorldSpaceFadeSlope,
                    _NoiseWarpZoomStrength, _NoiseWarpSkewStrength);
                alpha *= noise;
                #endif

                float preSquareAlpha = alpha;
                alpha *= alpha;
                // fb0a3537: height fog is saturate followed by cubic smoothstep.
                #if defined(HEIGHT_FOG)
                alpha *= CalculateParametricHeightRamp(
                    i.worldPos.y, _FogHeightScale, _FogHeightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);
                #endif
                float3 cameraPosition = GetParametricCameraPosition();

                float fogInverse = 1.0;
                // f68fe436/10c6243c: distance transmission receives pre-square alpha as divisor.
                #if defined(BLOOM_FOG)
                fogInverse = CalculateParametricDistanceTransmission(
                    i.worldPos, cameraPosition, _FogStartOffset, _FogScale, preSquareAlpha,
                    _CustomFogOffset, _CustomFogAttenuation);
                alpha *= fogInverse;
                #endif

                float3 surfaceAddition = 0.0;
                #if defined(REFLECTION_PROBE)
                // 866b1448/032dfb9c: the game reflects the view ray and samples
                // both spec cubes at roughness * (1.7 - 0.7 * roughness) * 6 mip,
                // decoding their six packed channels with the corresponding
                // light-bake ID rows, then saturating before reflection intensity.
                float3 incident = normalize(i.worldPos - cameraPosition);
                float3 worldNormal = normalize(i.worldNormal);
                float3 reflectionDirection = reflect(incident, worldNormal);
                surfaceAddition = saturate(SampleReflectionProbePair(
                    reflectionDirection, _Smoothness,
                    _ReflectionProbeTexture1, _ReflectionProbeTexture2,
                    _LightProbeLightBakeIdA, _LightProbeLightBakeIdB,
                    _LightProbeLightBakeIdC, _LightProbeLightBakeIdD,
                    _LightProbeLightBakeIdE, _LightProbeLightBakeIdF,
                    _ReflectionIntensity));
                #endif

                float3 rgb = color.rgb * alpha;
                // f68fe436: MAIN_EFFECT_ENABLED disables the source white-boost route.
                #if !defined(POST_BLOOM)
                rgb = CalculateBloomComposition(color.rgb, alpha, alpha, 1,
                                                _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                // 866b1448: the reflection contribution is attenuated by the
                // squared fog inverse and _GlassOpacity, never by the source alpha.
                rgb += surfaceAddition * fogInverse * fogInverse * _GlassOpacity;
                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}