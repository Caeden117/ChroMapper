// Replacement for the Beat Saber game shader Custom/OpaqueNeonLight.
Shader "ChroMapper/Parametric Box Opaque"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // PBO1. The 1.42.2 Custom/OpaqueNeonLight Properties block is authoritative:
    //       only five fog controls are material properties. _Color remains an
    //       instanced runtime input; shared fog, bloom, noise, camera, and
    //       post-process inputs remain globals.
    // PBO2 [3e0bbd17e77c38d1]: POSITION is the only mesh input. The vertex route
    //       outputs ordinary clip/world position, bloom-fog screen position, and
    //       object-randomized blue-noise screen position. Instancing and stereo
    //       select the runtime color and camera matrices.
    // PBO3 [814cc1498a56699a,e98a1337526a77a9]: source alpha is _Color.a squared.
    //       HEIGHT_FOG multiplies it by the shared cubic height ramp.
    // PBO4 [3472619767b58962,6bce22b0123d38cd]: BLOOM_FOG additionally multiplies
    //       alpha by distance transmission using _Color.a as divisor. Fog blend
    //       is 1 - heightRamp * a second transmission using final alpha.
    // PBO5 [2c0168399e26d562,c109313148f71b48]: without BLOOM_FOG, height fog
    //       still blends toward 0.1 with factor 1 - heightRamp.
    // PBO6 [d8ea76eac9bb6f18,e98a1337526a77a9]: when MAIN_EFFECT_ENABLED is off,
    //       white boost is alpha^2 * _BaseColorBoost - threshold. ChroMapper maps
    //       MAIN_EFFECT_ENABLED to POST_BLOOM and reuses CalculateBloomComposition.
    // PBO7. Blue-noise dithering is unconditional. The final formula is
    //       2 * source + fogBlend * (fogTarget - source), where fogTarget is the
    //       bloom pre-pass under BLOOM_FOG and 0.1 otherwise. It is not a lerp
    //       from the doubled source.
    // PBO8. ACES_TONE_MAPPING appears in every recorded route but produces no
    //       ACES operation or distinct branch, so no ACES route is implemented.
    //       OVERDRAW_VIEW remains intentionally omitted.
    // PBO9. Stage binaries cannot prove ShaderLab state. The established opaque
    //       Back/LEqual/ZWrite On replacement state is retained and hardcoded.
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

            // Game variants 39480f3a/8605c565 use ENABLE_HEIGHT_FOG as the only
            // local fragment keyword (ChroMapper drops the game's ENABLE_ prefix).
            // There is no white-boost selector.
            #pragma shader_feature_local_fragment HEIGHT_FOG
            // Global: enabled by the bloom-fog renderer during its pass.
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile _ POST_BLOOM

            #include "UnityCG.cginc"
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

                // Vertex routes 39480f3a/8605c565 perform the ordinary object
                // transform; their extra outputs are stereo/fog coordinates.
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

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float alpha = color.a * color.a;
                float heightFactor = 1.0;

                // Game fragment routes 6bce22b0 and d8ea76ea use the smooth
                // height ramp before distance transmission.
                #if defined(HEIGHT_FOG)
                heightFactor = CalculateParametricHeightRamp(
                    i.worldPos.y, _FogHeightScale, _FogHeightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);
                #endif

                float3 cameraPosition = GetStereoAwareCameraPosition();
                float fogTransmission = 1.0;

                // Game fragment routes 34726197/6bce22b0 use color.a as the
                // first divisor, then the resulting alpha as the second.
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
                // Game routes e98a1337/2c016839 apply the local white boost
                // without MAIN_EFFECT_ENABLED; POST_BLOOM disables that term.
                #if !defined(POST_BLOOM)
                rgb = CalculateBloomComposition(color.rgb, alpha, alpha, 1,
                                                _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                // Blue-noise sampling is unconditional in game routes
                // 814cc149/e98a1337; the shader has no NOISE_DITHERING keyword.
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