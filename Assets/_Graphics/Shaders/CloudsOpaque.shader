// ChroMapper/Clouds Opaque
// Replacement for the Beat Saber game shader Custom/CloudsOpaque (BTS clouds).
//
// _Offset remains available for material compatibility but has no shader consumer.
// DIFFUSE uses the shared five-light route, including BOTH_SIDES and INVERT, without private or ambient lighting.
// The swirl phase uses _Speed * (_Time.y + _TimeHelperOffset.y).
// _WorldNoiseScrolling.xy scrolls with (_Time.x + _TimeHelperOffset.x), without _Speed.
// Fog and the normal use the pre-noise world position. Only clip position receives world-noise displacement.
// NOISE_DITHERING scales projected coordinates with _GlobalBlueNoiseParams and adds (blueNoise - 0.5) / 255.
// Ordinary fog is 1 - heightFade.
// BLOOM_FOG samples _BloomPrePassTexture at screenPos.xy / screenPos.w and uses 1 - heightFade * distanceFade.
// ACES runs after lit saturation and before fog and dither. Alpha is always zero.
// Debug and white-boost routes are omitted.
// Vertex:
//   phase  = sin(v.vertex.z * 12.345)
//   wave   = sign(-phase) * (phase * 0.5 + 1.0) * _WorldNoiseIntensityScale
//   angle  = (v.vertex.x + wave * _Speed *
//             (_Time.y + _TimeHelperOffset.y)) / v.vertex.z
//            (time rotates the swirl)
//   pos    = (sin(angle) * v.z, v.y, cos(angle) * v.z)    (swirl around Y)
//   world  = ObjectToWorld * pos
//   pos.y += tex2Dlod(_NoiseTex, world.xz * _NoiseTex_ST + scroll).x
//            * _WorldNoiseScale + _WorldNoiseIntensityOffset   (world noise)
//   scroll = _WorldNoiseScrolling.xy * (_Time.x + _TimeHelperOffset.x)
// Outputs world position, inverted world-origin normal (-normalize(world)), the
// main texture UV, and projected coordinates for fog and blue-noise sampling.
// Fragment:
//   distFade = 1 / (1 + max(0, max(0, dist2 - customOffset) *
//                customAttenuation - _FogStartOffset) * _FogScale)
//   heightFade = smoothstep vertical band using the custom height globals
//   color = saturate(tex * vertexColor * light) with light = 5-light sum
//   NOISE_DITHERING: color += (blueNoise(scaledProjectedPosition)-0.5)/255
//   (The recovered shader uses the global blue-noise texture.) alpha = 0.
Shader "ChroMapper/Clouds Opaque"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [ShowIfAny(WORLD_NOISE)] _NoiseTex ("Noise Texture", 2D) = "white" {}

        [Space(20)]
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScale ("World Noise Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityScale ("World Noise Intensity Scale", float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityOffset ("World Noise Intensity Offset", float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0, 0, 0, 1)

        [Space(20)]
        [ShowIfAny(WORLD_NOISE)] _Speed ("Speed", float) = 1
        _Offset ("Offset", float) = 0

        [Space(20)]
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", float) = 0
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", float) = 1
        [ShowIfAny(FOG)] _HeightFogOffset ("Height Fog Offset", float) = 1

        [Space(20)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", float) = 2

        [Space(20)]
        [Toggle(DIFFUSE)] _EnableDiffuse ("Enable Diffuse", float) = 1
        [ToggleShowIfAny(BOTH_SIDES_DIFFUSE, DIFFUSE)] _EnableBothSidesDiffuse ("Both Sides Diffuse", float) = 0
        [ToggleShowIfAny(INVERT_DIFFUSE_NORMAL, DIFFUSE)] _InvertDiffuseNormal ("Invert Diffuse Normal", float) = 0
        [Toggle(WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", float) = 0
        [Toggle(FOG)] _EnableFog ("Enable Fog", float) = 1
        [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Cull [_CullMode]
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment BOTH_SIDES_DIFFUSE
            #pragma shader_feature_local_vertex WORLD_NOISE
            #pragma shader_feature_local_fragment INVERT_DIFFUSE_NORMAL
            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment NOISE_DITHERING
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Core/Tonemapping.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float3 world : TEXCOORD2;
                float3 nor : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float4 noiseScreenPos : TEXCOORD5;
                float4 position : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float4 _TimeHelperOffset;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityScale;
            float _WorldNoiseIntensityOffset;
            float4 _WorldNoiseScrolling;
            float _Speed;
            float _Offset;
            float _FogStartOffset;
            float _FogScale;
            float _HeightFogOffset;

            #if defined(SHADER_STAGE_VERTEX) && defined(WORLD_NOISE)
            float _GlobalRandomValue;
            #endif

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // vertex-1effd9ad (1effd9ad): phase, swirl, world-noise sample,
                // and the pre-noise world position passed to the fragment.
                float phase = sin(v.vertex.z * 12.345);
                float wave = sign(phase) * (phase * 0.5 + 1.0);

                // The swirl rides _Time.y + _TimeHelperOffset.y (cb0[15].y +
                // cb0[159].y in the recovered vertex).
                float angle = (v.vertex.x + wave * _Speed *
                    (_Time.y + _TimeHelperOffset.y)) / v.vertex.z;
                float3 pos = float3(sin(angle) * v.vertex.z, v.vertex.y, cos(angle) * v.vertex.z);

                // recovered: the fog position and the normal use the pre-noise
                // world position; only the clip position is noise-displaced
                float3 world = mul(unity_ObjectToWorld, float4(pos, 1.0)).xyz;
                o.world = world;
                o.nor = -normalize(world);

                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.color = v.color;

                #if defined(WORLD_NOISE)
                float2 scroll = _WorldNoiseScrolling.xy *
                    (_Time.x + _TimeHelperOffset.x);
                float2 nuv = world.xz * _NoiseTex_ST.xy + _NoiseTex_ST.zw + scroll;
                float noise = tex2Dlod(_NoiseTex, float4(nuv, 0, 0)).x;
                float displacement = noise * _WorldNoiseIntensityScale + _WorldNoiseIntensityOffset;
                world = mul(unity_ObjectToWorld,
                            float4(pos.x, pos.y + displacement, pos.z, 1.0)).xyz;
                #endif

                o.position = mul(unity_MatrixVP, float4(world, 1.0));
                o.screenPos = ComputeScreenPosCustom(o.position);
                #if defined(SHADER_STAGE_VERTEX) && defined(WORLD_NOISE)
                o.noiseScreenPos = ComputeNonStereoScreenPos(o.position);
                o.noiseScreenPos.xy = o.noiseScreenPos.xy * _GlobalBlueNoiseParams
                    + o.position.w * _GlobalRandomValue;
                o.noiseScreenPos.xy += float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13);
                #else
                o.noiseScreenPos = o.screenPos;
                o.noiseScreenPos.xy *= _GlobalBlueNoiseParams;
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 cameraPosition = _WorldSpaceCameraPos;
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                cameraPosition = unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
                #endif
                float3 d = i.world - cameraPosition;
                // fragment-119bc5fd (119bc5fd): custom distance fog is applied
                // after the local start offset and attenuation, not as a direct
                // `dist2 * scale + offset` expression.
                float dist2 = dot(d, d);
                float distFade = 1.0 / (1.0 +
                    max(0.0, max(0.0, dist2 - _CustomFogOffset) *
                        _CustomFogAttenuation - _FogStartOffset) * _FogScale);

                // recovered: t = clamp((world.y + _HeightFogOffset - band) / band),
                // then smoothstep-style t*t*(3-2t)
                // fragment-72154a (72154a52): ordinary FOG uses this height
                // ramp. fragment-119bc5fd adds the distance term for BLOOM_FOG.
                float hFade = CalculateCustomHeightFogFactor(
                    i.world, _HeightFogOffset, 1.0);
                float fade = 1.0 - distFade * hFade;

                #if defined(DIFFUSE) && defined(INVERT_DIFFUSE_NORMAL)
                float3 normal = i.nor;
                #else
                float3 normal = normalize(i.nor);
                #endif
                #if defined(INVERT_DIFFUSE_NORMAL)
                normal = -normal;
                #endif
                float3 color = tex2D(_MainTex, i.uv).rgb * i.color.rgb;
                #if defined(DIFFUSE)
                // fragment-119bc5fd (119bc5fd): the game sums five directional
                // lights against the inverted world normal; no ambient term.
                color *= CalculateLightDiffuse(normal);
                color = saturate(color);
                #endif

                #if defined(ACES_TONE_MAPPING)
                color = ApplyAcesTonemapping(float4(color, 0.0)).rgb;
                #endif

                #if defined(FOG)
                #if defined(BLOOM_FOG)
                // fragment-119bc5fd: Fog.hlsl applies the centered texture ratio.
                color = lerp(color, SampleBloomPrePass(i.screenPos).rgb, fade);
                #else
                // fragment-72154a52: ordinary fog lerps toward 0.1 with
                // 1 - t*t*(3-2*t) as the factor (inverse of the height ramp).
                color = lerp(color, 0.1.xxx, 1.0 - hFade);
                #endif
                #endif

                #if defined(NOISE_DITHERING)
                // fragment-19822184: additive blue-noise dither is independent
                // of fog; PostProcess.hlsl supplies the /255 term.
                float4 result = ApplyNoiseDither(
                    float4(color, 0), i.noiseScreenPos, _GlobalBlueNoiseTex);
                #else
                float4 result = float4(color, 0);
                #endif

                // fragment-40070c00: all cloud routes clear alpha.
                return result;
            }
            ENDHLSL
        }
    }
}
