// Replacement for the Beat Saber game shader Custom/Mirror.
Shader "ChroMapper/Mirror"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // M1. The 1.42.2 Custom/Mirror Properties block is authoritative. Runtime
    //     lightmap textures/bake IDs remain uniforms, not material properties.
    // M2 [vertex-c51879856e2e11c7]: POSITION and UV0 drive all normal routes;
    //     LIGHTMAP also reads UV1. Normal UV and scrolling are calculated with
    //     (_Time.x + _TimeHelperOffset.x) before interpolation.
    // M3 [8d8f776efaca7ef8,f6c1f193c04091cf]: normal red is multiplied by alpha,
    //     then XY is unpacked to [-1,1]. Detail scale tiles its UV and detail
    //     intensity blends the two unpacked XY values before bump distortion.
    // M4. Reflection UV is projected screen UV minus bumped XY multiplied by
    //     (worldPos-cameraPos).y / distance. _ReflectionIntensity is squared and
    //     scales the complete sampled RGBA value.
    // M5 [c8c83176642bd0a5,f6c1f193c04091cf]: supported composition is optional
    //     DIFFUSE or LIGHTMAP plus reflection. Reflection alpha is retained.
    //     SPECULAR remains exposed by the property contract but has no 1.44 binary.
    // M6 [a2cdd3ecb791db4e]: game ENABLE_DIRT is normalized to ChroMapper's DIRT;
    //     dirt = 1 + intensity * (sample - 1), multiplied into the full result.
    // M7 [705d3d71f8b20274]: ENABLE_BLOOM_FOG maps to BLOOM_FOG and lerps full
    //     RGBA. Blue-noise dithering is unconditional and adds (noise-0.5)/255.
    // M8. BEATGAMES_STEREO_PASS targets the game's stereo reflection atlas;
    //     ChroMapper instead renders a per-camera reflection texture. That route,
    //     general INSTANCING_ON, white boost, and OVERDRAW_VIEW remain omitted.
    // M9. Stage binaries cannot prove ShaderLab state. Established opaque queue,
    //     back-face culling, LEqual, ZWrite On, and stencil controls remain.
    Properties
    {
        _NormalTex ("Normal Texture", 2D) = "white" {}
        _BumpIntensity ("Bump Intensity", float) = 0.1
        _ReflectionIntensity ("Reflection Intensity", float) = 0.5
        _TextureScrolling ("Texture Scrolling", Vector) = (0,0,0,0)
        [Space] _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Toggle(DETAIL_NORMAL_MAP)] _DetailNormalMap ("Detail Normal Map", float) = 0
        [ShowIfAny(DETAIL_NORMAL_MAP)] _DetailNormalTextureScale ("Detail Normal Texture Scale", float) = 1
        [ShowIfAny(DETAIL_NORMAL_MAP)] _DetailNormalIntensity ("Detail Normal Intensity", float) = 0
        [ShowIfAny(DETAIL_NORMAL_MAP)] _DetailNormalTexScrolling ("Detail Scrolling", Vector) = (0.05,2,0,0)

        [Toggle(LIGHTMAP)] _EnableLightmap ("Enable Lightmap", float) = 0
        [Toggle(DIFFUSE)] _EnableDiffuse ("Enable Diffuse", float) = 0
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Enable Light Falloff", float) = 0
        [Toggle(SPECULAR)] _EnableSpecular ("Enable Specular", float) = 0
        [ShowIfAny(SPECULAR)] _SpecularIntensity ("Specular Intensity", float) = 1

        [Space(18)]
        [Toggle(DIRT)] _EnableDirt ("Enable Dirt", float) = 0
        [ShowIfAny(DIRT)] _DirtTex ("Dirt Texture", 2D) = "white" {}
        [ShowIfAny(DIRT)] _DirtIntensity ("Dirt Intensity", float) = 1

        [Space(18)]
        _TintColor ("Tint Color", Vector) = (1,1,1,1)

        [Space]
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1

        [PerRendererData] _ReflectionTex ("Reflection Texture", 2D) = "white" {}
        [Space(12)] _StencilRefValue ("Stencil Ref Value", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
            "DisableBatching"="True"
        }

        LOD 200
        Cull Back
        ZTest LEqual
        ZWrite On

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
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment LIGHTMAP
            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment LIGHT_FALLOFF

            #pragma shader_feature_local_fragment DETAIL_NORMAL_MAP
            #pragma shader_feature_local_fragment DIRT

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            sampler2D _NormalTex;
            float4 _NormalTex_ST;

            float _DetailNormalTextureScale;
            float _DetailNormalIntensity;
            float2 _DetailNormalTexScrolling;

            float4 _TintColor;
            float _Metallic;
            float _Smoothness;
            float _SpecularIntensity;

            float _BumpIntensity;
            float _ReflectionIntensity;
            float2 _TextureScrolling;

            sampler2D _DirtTex;
            float4 _DirtTex_ST;
            float _DirtIntensity;

            sampler2D _ReflectionTex;

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
            float _FogScale;
            float4 _TimeHelperOffset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                float4 reflectionPos : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float2 lightmapUV : TEXCOORD5;
                float4 noiseScreenPos : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.uv = i.uv;
                o.lightmapUV = i.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                o.reflectionPos = ComputeScreenPos(o.vertex);
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                o.noiseScreenPos = BuildNoiseScreenPosition(
                    o.screenPos, o.vertex, _GlobalBlueNoiseParams,
                    _GlobalRandomValue, unity_ObjectToWorld._m03_m13);

                return o;
            }

            float3 CalculateMirrorDiffuseLighting(float3 normal, float3 worldPos)
            {
                #if defined(LIGHT_FALLOFF)
                return CalculateLightFalloffDiffuse(worldPos, normal);
                #else
                return CalculateLightDiffuse(normal);
                #endif
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float mirrorTime = _Time.x + _TimeHelperOffset.x;
                float2 normalUV = TRANSFORM_TEX(i.uv, _NormalTex) + _TextureScrolling.xy * mirrorTime;
                float4 normalSample = tex2D(_NormalTex, normalUV);
                normalSample.x *= normalSample.w;
                float2 normalXY = normalSample.xy * 2 - 1;

                #if defined(DETAIL_NORMAL_MAP)
                float2 detailUV = (normalUV + _DetailNormalTexScrolling.xy * mirrorTime) *
                    _DetailNormalTextureScale;
                float4 detailSample = tex2D(_NormalTex, detailUV);
                detailSample.x *= detailSample.w;
                float2 detailXY = detailSample.xy * 2 - 1;
                normalXY = lerp(normalXY, detailXY, _DetailNormalIntensity);
                #endif

                float3 diffuseNormalTangent = float3(
                    normalXY,
                    max(sqrt(1 - min(dot(normalXY, normalXY), 1)), 1e-16));
                float2 reflectionNormalXY = normalXY * _BumpIntensity;

                float3 eyeCameraPosition = _WorldSpaceCameraPos;
                #if defined(USING_STEREO_MATRICES)
                eyeCameraPosition = unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
                #endif
                float3 toCamera = i.worldPos - eyeCameraPosition;
                float viewY = toCamera.y / max(length(toCamera), 1e-16);
                float2 reflectionUV = i.reflectionPos.xy / i.reflectionPos.w -
                    reflectionNormalXY * viewY;
                float reflectionIntensity = _ReflectionIntensity * _ReflectionIntensity;
                float4 reflectionCol = tex2D(_ReflectionTex, reflectionUV) * reflectionIntensity;

                float3 lighting = 0;
                #if defined(DIFFUSE)
                lighting += CalculateMirrorDiffuseLighting(diffuseNormalTangent, i.worldPos) *
                    (1 - _Metallic) * _TintColor.rgb;
                #endif

                #if defined(LIGHTMAP)
                float3 lightmap1 = tex2D(_LightMap1, i.lightmapUV).rgb;
                float3 lightmap2 = tex2D(_LightMap2, i.lightmapUV).rgb;
                float3 decodedLightmap =
                    lightmap1.r * _LightmapLightBakeIdA +
                    lightmap1.g * _LightmapLightBakeIdB +
                    lightmap1.b * _LightmapLightBakeIdC +
                    lightmap2.r * _LightmapLightBakeIdD +
                    lightmap2.g * _LightmapLightBakeIdE +
                    lightmap2.b * _LightmapLightBakeIdF;
                // Original D3D lightmaps use 0x4093088c; preserve other backends until their coefficients are verified.
                #if defined(SHADER_API_D3D11)
                const float mirrorLightmapDecodeScale = 4.5947933;
                #else
                const float mirrorLightmapDecodeScale = 4.594793;
                #endif
                lighting += decodedLightmap * mirrorLightmapDecodeScale * (1 - _Metallic) * _TintColor.rgb;
                #endif

                #if defined(ACES_TONE_MAPPING) && (defined(DIFFUSE) || defined(LIGHTMAP))
                float4 lightingColor = float4(lighting, 0);
                lightingColor = ApplyAcesTonemapping(lightingColor);
                lighting = lightingColor.rgb;
                #endif

                float4 albedo = reflectionCol;
                #if defined(DIFFUSE) || defined(LIGHTMAP)
                albedo = float4(lighting + reflectionCol.rgb, reflectionCol.a);
                #endif

                #if defined(DIRT)
                float4 dirt = tex2D(_DirtTex, TRANSFORM_TEX(i.uv, _DirtTex));
                dirt = 1 + _DirtIntensity * (dirt - 1);
                albedo *= dirt;
                #endif

                #if defined(BLOOM_FOG)
                albedo = ApplyBloomFog(albedo, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif

                albedo = ApplyNoiseDither(albedo, i.noiseScreenPos, _GlobalBlueNoiseTex);

                return albedo;
            }
            ENDHLSL
        }
    }
    Fallback "Diffuse"
}