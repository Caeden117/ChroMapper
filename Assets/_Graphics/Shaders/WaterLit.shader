// Replacement for the Beat Saber game shader Custom/WaterLit.
Shader "ChroMapper/Water Lit"
{
    // Importer aliases retain _NormalTex and _BlendMode* property bindings.
    // Normal UVs are (uv + scroll * gameTime) * ST.xy. Detail scale tiles its UV,
    // and detail intensity blends two unpacked normals. _NormalScale blends the
    // final mapped world normal, while _NormalScaleVertical scales tangent XY by
    // 1 + value * (1 - normal.y).
    // World tangent and bitangent are formed per vertex and interpolated separately.
    // Water uses the packed reflection cubes when BakedReflectionProbe publishes a
    // complete pair, otherwise retaining the Unity probe fallback. Lightmap diffuse
    // uses the 4.59479332 * (1-metallic) * color factor.
    // Falling fog adds _FallingFogStartOffset * (1-saturate(normal.y)) to _FogStartOffset.
    // Bloom height fog changes RGB only. Noise adds
    // (blueNoise.r - 0.5) / 255 after fog.
    // Material properties drive blend, cull, Z-write, and stencil state.
    Properties
    {
        _Color ("Color", Vector) = (1,1,1,1)

        [Header(Base Properties)]
        [Toggle(METAL_SMOOTHNESS_TEXTURE)] _EnableMetalSmoothnessTex ("Use Metallic & Smoothness Texture", Float) = 0
        _MetalSmoothnessTex ("Metallic(R) & Smoothness(A) Texture", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Toggle(SPECULAR_ANTIFLICKER)] _SpecularAntiflicker ("Smoothness Anti-Flicker", Float) = 0
        _AntiflickerStrength ("Antiflicker Strength", Range(0, 1)) = 0.7
        _AntiflickerDistanceScale ("Antiflicker Distance Scale", Float) = 0.1
        _AntiflickerDistanceOffset ("Antiflicker Distance Offset", Float) = 21
        [KeywordEnum(None, Color, Emission, MetalSmoothness, Special)] _VertexMode ("Vertex Color Mode", Float) = 0
        _EmissionThreshold ("Emission Threshold", Range(0, 1)) = 0
        _EmissionColor ("Emission Color", Vector) = (1,1,1,0)

        [Toggle(Z_FADE)] _ZFade ("Z Fade", Float) = 0
        _ZFadePosition ("Z Fade Position", Float) = 0
        _ZFadeScale ("Z Fade Scale", Float) = 1
        [Toggle(Y_FADE)] _YFade ("Y Fade", Float) = 0
        _YFadePosition ("Y Fade Position", Float) = 0
        _YFadeScale ("Y Fade Scale", Float) = 1

        [Header(Emissions And Decals)]
        [KeywordEnum(None, Simple, Pulse)] _EmissionTexture ("Emission Texture", Float) = 0
        _EmissionBrightness ("Brightness", Float) = 1
        [KeywordEnum(Flat, Whiteboost, Gradient)] _EmissionColorType ("Color Type", Float) = 0
        _EmissionTexColor ("Emission Color", Vector) = (1,1,1,0)
        _EmissionGradientTex ("Gradient LUT", 2D) = "white" {}
        _EmissionTex ("Emission Texture", 2D) = "white" {}
        _EmissionTexSpeed ("Texture Speed", Vector) = (0,0,0,0)
        [Toggle(EMISSION_TWICE)] _EmissionSampleTwice ("Sample Twice", Float) = 0
        _Emission2Tiling ("2nd Sample Tiling", Vector) = (1,1,0,0)
        _Emission2Speed ("2nd Sample Speed", Vector) = (0,0,0,0)
        _PulseMask ("Pulse Mask", 2D) = "white" {}
        [Toggle(INVERT_PULSE)] _InvertPulseTexture ("Invert Texture", Float) = 0
        [Toggle(PULSE_MULTIPLY_TEXTURE)] _PulseMultiplyByTexture ("Brightness from Texture", Float) = 0
        _PulseWidth ("Pulse Width", Float) = 0.1
        _PulseSpeed ("Pulse Speed", Float) = 0.2
        _PulseSmooth ("Pulse Smooth", Range(0, 0.2)) = 0.02
        [Toggle(EMISSION_MASK)] _EnableEmissionMask ("Use Emission Mask", Float) = 0
        _EmissionMask ("Mask Texture", 2D) = "white" {}
        _EmissionMaskSpeed ("Mask Texture Speed", Vector) = (0,1,0,0)
        [KeywordEnum(None, Lerp, Additive)] _RimLight ("Rim Light Type", Float) = 0
        _RimLightEdgeStart ("Rim Light Edge Start", Float) = 0.5
        _RimLightColor ("Rim Light Color", Vector) = (1,1,1,0)
        _RimLightIntensity ("Rim Light Intensity", Float) = 1

        [Header(Lighting)]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Enable Diffuse", Float) = 1
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Enable Light Falloff", Float) = 0
        [Toggle(INVERT_DIFFUSE_NORMAL)] _InvertDiffuseNormal ("Invert Diffuse Normal", Float) = 0
        [Toggle(BOTH_SIDES_DIFFUSE)] _EnableBothSidesDiffuse ("Enable Both Sides Diffuse", Float) = 0
        [Toggle(PRIVATE_POINT_LIGHT)] _PrivatePointLight ("Private Point Light", Float) = 0
        [HDR] _PrivatePointLightColor ("Color", Vector) = (1,0,0,0)
        [Toggle(POINT_LIGHT_IS_LOCAL)] _PointLightPositionLocal ("Make Position Local", Float) = 0
        _PrivatePointLightPosition ("Light World Position", Vector) = (0,0,0,1)
        [Toggle(DIFFUSE_TEXTURE)] _EnableDiffuseTexture ("Enable Albedo Texture", Float) = 0
        _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
        [Toggle(SPECULAR)] _EnableSpecular ("Enable Specular", Float) = 1
        _SpecularIntensity ("Specular Intensity", float) = 1

        [Toggle(LIGHTMAP)] _EnableLightmap ("Enable Lightmap", Float) = 0
        [Toggle(NORMAL_MAP)] _EnableNormalMap ("Enable Normal Map", float) = 0
        _NormalTex ("Normal Texture", 2D) = "bump" {}
        _NormalScale ("Normal Scale", float) = 1
        _NormalScaleVertical ("Falling Normal Scale", float) = 1
        _NormalTexScrolling ("Texture Scrolling", Vector) = (0,2,0,0)
        [Toggle(DETAIL_NORMAL_MAP)] _DetailNormalMap ("Detail Normal Map", float) = 0
        _DetailNormalTextureScale ("Detail Normal Texture Scale", float) = 1
        _DetailNormalIntensity ("Detail Normal Intensity", float) = 0
        _DetailNormalTexScrolling ("Detail Scrolling", Vector) = (0.05,2,0,0)
        [Toggle(USE_SPHERICAL_NORMAL_OFFSET)] _UseSphericalNormalOffset ("Spherical Normal Offset", Float) = 0
        _SphericalNormalOffsetIntensity ("Spherical Normal Offset Intensity", Float) = 0.5
        _SphericalNormalOffsetCenter ("Spherical Normal Offset Center", Vector) = (0,0,0,1)

        [Header(Reflections)]
        [Toggle(REFLECTION_TEXTURE)] _EnableReflectionTexture ("Enable Reflection Texture", Float) = 0
        _ReflectionTexIntensity ("Texture Intensity", Float) = 1
        _EnvironmentReflectionCube ("Environment Reflection", Cube) = "" {}
        [Toggle(REFLECTION_PROBE)] _EnableReflectionProbe ("Enable Reflection Probe", Float) = 0
        _ReflectionProbeIntensity ("Probe Intensity", Float) = 1
        [Toggle(REFLECTION_PROBE_BOX_PROJECTION)] _ReflectionProbeBoxProjection ("Box Projection", Float) = 1
        [Toggle(REFLECTION_PROBE_BOX_PROJECTION_OFFSET)] _EnableBoxProjectionOffset ("Enable Box Projection Offset", Float) = 0
        _ReflectionProbeBoxProjectionSizeOffset ("Box Projection Size Offset", Vector) = (0,0,0,0)
        _ReflectionProbeBoxProjectionPositionOffset ("Box Projection Position Offset", Vector) = (0,0,0,0)
        [Toggle(ENABLE_RIM_DIM)] _EnableRimDim ("Reflection Rim Dim", Float) = 0
        _RimScale ("Rim Scale", Float) = 1
        _RimOffset ("Rim Offset", Float) = 1
        _RimDistanceOffset ("Rim Camera Distance Offset", Float) = 2
        _RimDistanceScale ("Rim Camera Distance Scale", Float) = 0.3
        _RimDarkening ("Rim Darkenning", Float) = 0
        [Toggle(INVERT_RIM_DIM)] _InvertRimDim ("Invert Rim Dim", Float) = 0

        [Header(Dirt And Ground Fade)]
        [Toggle(GROUND_FADE)] _EnableGroundFade ("Enable Ground Fade", Float) = 0
        _GroundFadeScale ("Ground Fade Scale", Float) = 0.5
        _GroundFadeOffset ("Ground Fade Offset", Float) = 1
        [Toggle(DIRT)] _EnableDirt ("Enable Dirt", Float) = 0
        _DirtTex ("Dirt Texture", 2D) = "white" {}
        _DirtIntensity ("Dirt Intensity", Float) = 1
        [Toggle(DIRT_DETAIL)] _EnableDirtDetail ("Enable Dirt Detail", Float) = 0
        _DirtDetailTex ("Dirt Detail Texture", 2D) = "white" {}
        _DirtDetailIntensity ("Dirt Detail Intensity", Float) = 1

        [Header(Other)]
        [KeywordEnum(None, 90_CW, 90_CCW, 180_CW)] _RotateUV ("Rotate UVs", Float) = 0

        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FallingFogStartOffset ("Falling Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        [Toggle(HEIGHT_FOG)] _EnableHeightFog ("Enable Height Fog", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
        _FogHeightOffset ("Fog Height Offset", float) = 0
        [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", float) = 0
        [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", float) = 1
        [Toggle(LINEAR_TO_GAMMA)] _LinearToGamma ("LinearToGamma", Float) = 0

        [Header(Settings)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", float) = 2
        [Toggle] _ZWrite ("Z Write", float) = 1
        _StencilRefValue ("Stencil Ref Value", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src Factor", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst Factor", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        LOD 200
        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        Cull [_CullMode]
        ZTest LEqual
        ZWrite [_ZWrite]

        Pass
        {
            Stencil
            {
                Ref [_StencilRefValue]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment Z_FADE
            #pragma shader_feature_local_fragment NORMAL_MAP
            #pragma shader_feature_local_fragment DETAIL_NORMAL_MAP
            #pragma shader_feature_local_fragment LIGHTMAP
            #pragma shader_feature_local_fragment NOISE_DITHERING
            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment REFLECTION_PROBE
            #pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION
            #pragma shader_feature_local_fragment REFLECTION_PROBE_BOX_PROJECTION_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSIONCOLORTYPE_FLAT
            #pragma shader_feature_local_fragment _ _DECALBLEND_ALPHABLEND

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #define _FOGTYPE_COLOR
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Reflection.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            float _Metallic;
            float _Smoothness;
            float _SpecularIntensity;
            float _ReflectionProbeIntensity;
            float _HasBakedReflectionProbeData;
            samplerCUBE _ReflectionProbeTexture1;
            samplerCUBE _ReflectionProbeTexture2;
            float3 _ReflectionProbeBoundsMin;
            float3 _ReflectionProbeBoundsMax;
            float3 _ReflectionProbePosition;
            float4 _LightProbeLightBakeIdA;
            float4 _LightProbeLightBakeIdB;
            float4 _LightProbeLightBakeIdC;
            float4 _LightProbeLightBakeIdD;
            float4 _LightProbeLightBakeIdE;
            float4 _LightProbeLightBakeIdF;
            float3 _ReflectionProbeBoxProjectionSizeOffset;
            float3 _ReflectionProbeBoxProjectionPositionOffset;

            float _ZFadePosition;
            float _ZFadeScale;

            sampler2D _NormalTex;
            float4 _NormalTex_ST;
            float _NormalScale;
            float _NormalScaleVertical;
            float2 _NormalTexScrolling;
            float _DetailNormalTextureScale;
            float _DetailNormalIntensity;
            float2 _DetailNormalTexScrolling;
            float4 _TimeHelperOffset;

            sampler2D _LightMap1;
            sampler2D _LightMap2;
            float3 _LightmapLightBakeIdA;
            float3 _LightmapLightBakeIdB;
            float3 _LightmapLightBakeIdC;
            float3 _LightmapLightBakeIdD;
            float3 _LightmapLightBakeIdE;
            float3 _LightmapLightBakeIdF;

            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float _GlobalRandomValue;

            float _FogStartOffset;
            float _FallingFogStartOffset;
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
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float3 worldTangent : TEXCOORD4;
                float2 lightmapUv : TEXCOORD5;
                float3 worldBitangent : TEXCOORD6;
                float4 noiseScreenPos : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                o.uv.xy = i.uv.xy;
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                o.lightmapUv = i.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                o.noiseScreenPos = BuildNoiseScreenPosition(
                    o.screenPos, o.vertex, _GlobalBlueNoiseParams,
                    _GlobalRandomValue, unity_ObjectToWorld._m03_m13);
                o.worldTangent = normalize(UnityObjectToWorldDir(i.tangent.xyz));
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) *
                    (i.tangent.w * unity_WorldTransformParams.w);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float3 worldPos = i.worldPos;

                #if defined(Z_FADE)
                albedo.a *= saturate((_ZFadePosition - worldPos.z) * _ZFadeScale);
                #endif

                float3 worldNormal = i.worldNormal;

                #if defined(NORMAL_MAP)
                float waterTime = _Time.x + _TimeHelperOffset.x;
                float2 normalUv = (i.uv + _NormalTexScrolling.xy * waterTime) * _NormalTex_ST.xy;
                float3 normalTangent = UnpackNormal(tex2D(_NormalTex, normalUv));
                #if defined(DETAIL_NORMAL_MAP)
                float2 detailNormalUv =
                    (i.uv + _DetailNormalTexScrolling.xy * waterTime) *
                    _NormalTex_ST.xy * _DetailNormalTextureScale;
                float3 detailNormalTangent = UnpackNormal(tex2D(_NormalTex, detailNormalUv));
                normalTangent = lerp(
                    normalTangent,
                    detailNormalTangent,
                    _DetailNormalIntensity);
                #endif
                float fallingNormalScale = 1.0 + _NormalScaleVertical * (1.0 - worldNormal.y);
                normalTangent.xy *= fallingNormalScale;
                float3x3 tbn = float3x3(i.worldTangent, i.worldBitangent, worldNormal);
                // Preserve mapped magnitude until the _NormalScale blend.
                float3 mappedWorldNormal = mul(normalTangent, tbn);
                worldNormal = normalize(lerp(worldNormal, mappedWorldNormal, _NormalScale));
                #endif

                float3 lighting = 0.0;

                #if defined(REFLECTION_PROBE)
                SurfaceData reflectionSurface = InitializeSurfaceData(
                    worldPos, worldNormal, i.uv, i.uv, albedo,
                    _Metallic, _Smoothness);
                if (_HasBakedReflectionProbeData > 0.5)
                {
                    float3 reflectionDirection = CalculateViewReflectionDirection(worldPos, worldNormal);
                #if defined(REFLECTION_PROBE_BOX_PROJECTION)
                    float3 boundsMin = _ReflectionProbeBoundsMin;
                    float3 boundsMax = _ReflectionProbeBoundsMax;
                    float3 probePosition = _ReflectionProbePosition;
                #if defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET)
                    boundsMin -= _ReflectionProbeBoxProjectionSizeOffset;
                    boundsMax += _ReflectionProbeBoxProjectionSizeOffset;
                    probePosition += _ReflectionProbeBoxProjectionPositionOffset;
                #endif
                    reflectionDirection = BoxProjectReflectionDirection(
                        reflectionDirection, worldPos, boundsMin, boundsMax, probePosition);
                #endif
                    float3 reflection = SampleReflectionProbePair(
                        reflectionDirection, _Smoothness,
                        _ReflectionProbeTexture1, _ReflectionProbeTexture2,
                        _LightProbeLightBakeIdA, _LightProbeLightBakeIdB,
                        _LightProbeLightBakeIdC, _LightProbeLightBakeIdD,
                        _LightProbeLightBakeIdE, _LightProbeLightBakeIdF,
                        _ReflectionProbeIntensity);
                    reflection *= 1.0 + _Metallic * (albedo.rgb - 1.0);
                    reflection *= 2.0 * (_Metallic * 0.8 + 0.2);
                    reflection *= _Smoothness;
                    lighting += reflection;
                }
                else
                {
                    float3 reflectionDirection = CalculateViewReflectionDirection(
                        reflectionSurface.worldPosition, worldNormal);
                #if defined(REFLECTION_PROBE_BOX_PROJECTION)
                float3 boundsMin = unity_SpecCube0_BoxMin.xyz;
                float3 boundsMax = unity_SpecCube0_BoxMax.xyz;
                float3 probePosition = unity_SpecCube0_ProbePosition.xyz;
                #if defined(REFLECTION_PROBE_BOX_PROJECTION_OFFSET)
                boundsMin -= _ReflectionProbeBoxProjectionSizeOffset;
                boundsMax += _ReflectionProbeBoxProjectionSizeOffset;
                probePosition += _ReflectionProbeBoxProjectionPositionOffset;
                #endif
                if (unity_SpecCube0_ProbePosition.w > 0.0)
                {
                    reflectionDirection = BoxProjectReflectionDirection(
                        reflectionDirection, reflectionSurface.worldPosition,
                        boundsMin, boundsMax, probePosition);
                }
                #endif

                float roughness = 1.0 - reflectionSurface.smoothness;
                float reflectionLod = roughness * (1.7 - 0.7 * roughness) * 6.0;
                half4 encodedReflection = UNITY_SAMPLE_TEXCUBE_LOD(
                    unity_SpecCube0, reflectionDirection, reflectionLod);
                float3 reflection = DecodeHDR(encodedReflection, unity_SpecCube0_HDR);
                reflection *= _ReflectionProbeIntensity;
                reflection *= 1.0 + reflectionSurface.metallic *
                    (reflectionSurface.baseColor.rgb - 1.0);
                reflection *= 2.0 * (reflectionSurface.metallic * 0.8 + 0.2);
                reflection *= reflectionSurface.smoothness;
                lighting += reflection;
                }
                #endif

                #if defined(LIGHTMAP)
                float3 lightmap1 = tex2D(_LightMap1, i.lightmapUv).rgb;
                float3 lightmap2 = tex2D(_LightMap2, i.lightmapUv).rgb;
                float3 decodedLightmap =
                    lightmap1.r * _LightmapLightBakeIdA +
                    lightmap1.g * _LightmapLightBakeIdB +
                    lightmap1.b * _LightmapLightBakeIdC +
                    lightmap2.r * _LightmapLightBakeIdD +
                    lightmap2.g * _LightmapLightBakeIdE +
                    lightmap2.b * _LightmapLightBakeIdF;
                lighting += decodedLightmap * 4.59479332 * (1.0 - _Metallic) * albedo.rgb;
                #endif

                albedo.rgb = lighting;

                #if defined(ACES_TONE_MAPPING)
                albedo = ApplyAcesTonemapping(albedo);
                #endif

                #if defined(BLOOM_FOG) && defined(FOG)
                float fogStartOffset = mad(
                    1.0 - saturate(worldNormal.y), _FallingFogStartOffset, _FogStartOffset);
                #if defined(HEIGHT_FOG)
                albedo = ApplyBloomHeightFog(albedo, i.screenPos, worldPos, fogStartOffset, _FogScale,
                                             _FogHeightOffset,
                                             _FogHeightScale);
                #else
                albedo = ApplyBloomFog(albedo, i.screenPos, worldPos, fogStartOffset, _FogScale);
                #endif
                #elif defined(FOG) && defined(HEIGHT_FOG)
                // Retain literal-endpoint height fog when bloom is off.
                float heightFogFactor = CalculateCustomHeightFogFactor(
                    worldPos, _FogHeightOffset, _FogHeightScale);
                albedo.rgb = (1.0 - heightFogFactor) * (float3(0.1, 0.1, 0.1) - albedo.rgb) + albedo.rgb;
                #endif

                #if defined(NOISE_DITHERING)
                albedo = ApplyNoiseDither(albedo, i.noiseScreenPos, _GlobalBlueNoiseTex);
                #endif

                return albedo;
            }
            ENDHLSL
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            Blend Off
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"

            float _ZWrite;

            struct appdataShadowCaster
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2fShadowCaster
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fShadowCaster vertShadowCaster(appdataShadowCaster v)
            {
                v2fShadowCaster o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2fShadowCaster, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadowCaster(v2fShadowCaster i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Transparent water must not occlude depth or color-encoded cube shadows.
                if (_ZWrite == 0.0) discard;
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDHLSL
        }
    }
}
