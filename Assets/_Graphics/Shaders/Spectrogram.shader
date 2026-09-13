// Replacement for the Beat Saber game shader Custom/Spectrogram.
Shader "ChroMapper/Spectrogram"
{
    // ToggleHeader uses Unity's standard Toggle attribute.
    // UV.x selects uint(max(uv.x * 63, 0)).
    // The vertex offset is -uv.y * (1-sample) * _PeakOffset.xyz before object-to-world and clip transforms.
    // POSITION, NORMAL, and UV0 supply the mesh inputs.
    // DIFFUSE, SPECULAR, and LIGHT_FALLOFF use the shared five-light equations.
    // Specular color is metallic * (diffuseLighting * color - 0.04) + 0.04.
    // Height fog is always evaluated. ENABLE_BLOOM_FOG maps to ChroMapper's BLOOM_FOG global.
    // It combines height-retained and distance-retained factors before sampling bloom fog.
    // Blue-noise dithering is unconditional: rgb += (blueNoise.r - 0.5) / 255. Output alpha is always zero.
    // White-boost, NOISE_DITHERING keyword variants, and the OVERDRAW_VIEW diagnostic route are omitted.
    Properties
    {
        _Color ("Color", Vector) = (1,1,1,1)
        _PeakOffset ("Peak Offset", Vector) = (0,10,0,1)

        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Space]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Enable Diffuse", float) = 1
        [Space(12)]
        [Toggle(SPECULAR)] _EnableSpecular ("Enable Specular", float) = 1
        [ShowIfAny(SPECULAR)]
        _SpecularIntensity ("Specular Intensity", float) = 1
        [Space(12)]
        [Toggle(LIGHT_FALLOFF)] _EnableLightFalloff ("Enable Light Falloff", float) = 0

        [Space(12)]
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1

        [Space(12)]
        [Toggle] _ZWrite ("Z Write", float) = 1
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
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "ShaderLibrary/Families/SpectrogramShared.hlsl"

        float _SpectrogramData[64];
        float3 _PeakOffset;

        // The camera depth texture uses the caster, not the visible pass.
        // Both passes must deform the input mesh before their own projection.
        inline float4 DeformSpectrogramVertex(float4 position, float2 uv)
        {
            uint index = CalculateSpectrogramIndex(uv.x);
            position.xyz = position.xyz - uv.y *
                (1.0 - _SpectrogramData[index]) * _PeakOffset.xyz;
            return position;
        }
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment SPECULAR
            #pragma shader_feature_local_fragment LIGHT_FALLOFF

            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            float _Smoothness;
            float _Metallic;
            float _SpecularIntensity;

            float _FogStartOffset;
            float _FogScale;

            float4 _Color;
            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float _GlobalRandomValue;

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
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float4 noiseScreenPos : TEXCOORD4;
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

                i.vertex = DeformSpectrogramVertex(i.vertex, i.uv);

                o.vertex = UnityObjectToClipPos(i.vertex);
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                o.uv.xy = i.uv.xy;
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

                float3 lighting = 0;
                #if defined(DIFFUSE)
                #if defined(SPECULAR)
                #if defined(LIGHT_FALLOFF)
                float3 diffuseLighting = CalculateLightFalloffDiffuse(i.worldPos, i.worldNormal);
                float3 specularLighting = CalculateLightFalloffSpecular(i.worldPos, i.worldNormal, _Smoothness);
                #else
                float3 diffuseLighting = CalculateLightDiffuse(i.worldNormal);
                float3 specularLighting = CalculateLightSpecular(i.worldPos, i.worldNormal, _Smoothness);
                #endif
                float3 diffuseColor = diffuseLighting * _Color.rgb;
                float3 specularColor = _Metallic * (diffuseColor - 0.04) + 0.04;
                lighting = diffuseColor * (0.96 * (1.0 - _Metallic)) +
                    specularLighting * specularColor * _SpecularIntensity;
                #else
                #if defined(LIGHT_FALLOFF)
                lighting = CalculateLightFalloffDiffuse(i.worldPos, i.worldNormal) * _Color.rgb * (1.0 - _Metallic);
                #else
                lighting = CalculateLightDiffuse(i.worldNormal) * _Color.rgb * (1.0 - _Metallic);
                #endif
                #endif
                #endif

                float4 albedo = float4(lighting, 0);

                #if defined(ACES_TONE_MAPPING)
                albedo = ApplyAcesTonemapping(albedo);
                #endif

                float heightRetained = CalculateCustomHeightFogFactor(
                    i.worldPos, 0.0, 1.0);
                #if defined(BLOOM_FOG)
                float distanceFogFactor = CalculateCustomFogFactor(
                    distanceSquared(i.worldPos), _FogStartOffset, _FogScale);
                albedo = ApplyBloomFogCalculatedFactor(
                    albedo, i.screenPos, 1 - heightRetained * (1 - distanceFogFactor));
                #else
                albedo.rgb = lerp(0.1, albedo.rgb, heightRetained);
                #endif

                albedo = ApplyNoiseDither(albedo, i.noiseScreenPos, _GlobalBlueNoiseTex);
                albedo.a = 0;

                return albedo;
            }
            ENDHLSL
        }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            struct appdataShadowCaster
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
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
                v.vertex = DeformSpectrogramVertex(v.vertex, v.uv);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadowCaster(v2fShadowCaster i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDHLSL
        }
    }
    Fallback "Diffuse"
}
