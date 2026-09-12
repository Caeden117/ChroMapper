// ChroMapper/Clouds Lit Transparent
// Replacement for the Beat Saber game shader Custom/CloudsLitTransparent
// (billie environment clouds).
//
// The BillieClouds mesh supplies Position, Normal, Tangent, Color, TexCoord0, and TexCoord1.
// COLOR.rgb supplies three one-hot rotation-layer weights. TexCoord0/uv0 is the main cloud UV, not the weight channel.
// TexCoord1 does not carry rotation-layer weights.
// The instance-aware _Color property supplies the bake payload and the no-feature output.
// ACES remains keyworded. Bloom, main-effect, and unsupported debug routes are omitted.
// Fragment lighting reads directional-light RGB without an alpha gate or normalization of the interpolated lighting vector.
// Billie uses SrcAlpha/OneMinusSrcAlpha for RGB and Zero/One for alpha.
// Preserving destination alpha prevents an unlit cloud mask from whitening in the alpha-driven bloom post-process.
//
// Vertex (ALIGN_NORMALS_TO_WORLD_ORIGIN variant):
//   angle  = (weights.xyz · _RotateLayerSpeeds.xyz) *
//            (_Time.x + _TimeHelperOffset.x) * deg2rad
//            (per-particle rotation layers with a time offset from _TimeHelperOffset.x,
//            which is 0 for the Billie material.
//            The converted mesh supplies these weights through COLOR.rgb and
//            the main UV through TexCoord0/uv0.)
//   pos    = world pos rotated around the world Y axis: x' = c*x - s*z,
//            z' = s*x + c*z                    (swirl of the cloud sheet)
//   pos.y += sin(worldPos.x * _VertexWaveFrequency + _Time.z + helper.z)
//            * _VertexWaveAmplitude
//   out: v1 = rotated world pos (runway fade), v2 = world-origin normal
//        (i.e. -normalize(world xz), built from the world X/Z only, lit like a
//        plane facing away from origin)
// Fragment:
//   scroll   = (_Time.x + _TimeHelperOffset.x) * _DistortTexSpeed.xy * _DistortTex_ST.xy
//   uvDistort = uv * _DistortTex_ST + ST + scroll
//   d        = tex2D(_DistortTex, uvDistort)
//   uvMain   = uv * _DiffuseTex_ST + ST
//              + _DistortAmount * (d.r - uvMain.x, d.a - uvMain.y)
//   base     = tex2D(_DiffuseTex, uvMain)
//   light    = frontLobe + backLobe * _BackLightingBoost * base.g
//              (5 recovered directional lights; ChroMapper drives them from
//              its BiRP light rig _DirectionalLightDirections/Colors)
//   tint     = instance-aware _Color
//   bottom   = saturate((world.y - min) / (max - min)) ^ 2   (square, not smoothstep)
//   runway   = 1 - saturate(gate * _RunwayFadeScale / dist + _RunwayFadeOffset)
//              with dist = len(world.x, world.y-1, gate),
//              gate = (world.z > 0) ? 1 : 0   (positive half only;
//              on the other side the offset alone drives the fade)  (FADE_RUNWAY)
//   color    = base.b * light * tint.rgb * bottom     (blue channel tints the whole)
//   alpha    = base.a * runway * bottom * tint.a
//              (recovered: r2 = (1,1,1,base.a) * bake[instance] at asm 103;
//              base.a reaches o0.w through r2.w, not r0.w)
//
// The recovered features are exposed as [Toggle] properties with authoritative
// defaults. The attributes are required because ChroMapper's environment
// material pipeline enables keywords through [Toggle]/[KeywordEnum] properties;
// the all-off path remains an intentional transparent fallback.
// Fog properties remain serialized compatibility data. The recovered fragment
// corpus has no FOG or HEIGHT_FOG route. _DistortUVChannel is also exposed for
// compatibility and the recovered route uses channel zero.
Shader "ChroMapper/Clouds Lit Transparent"
{
    Properties
    {
        _Color ("Color", Vector) = (1,1,1,1)
        _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
        [ShowIfAny(DISTORT_TEXTURE)] _DistortTex ("Distort Texture", 2D) = "white" {}

        [Space(20)]
        _DistortTexSpeed ("Distort Tex Speed", Vector) = (0.04, 0, 0, 0)
        _DistortAmount ("Distort Amount", Range(0, 0.1)) = 0.1
        [EnumShowIfAny(2, Zero, One, DISTORT_TEXTURE)] _DistortUVChannel ("Distort UV Channel", float) = 0

        [Space(20)]
        _BackLightingBoost ("Backlighting Boost", float) = 2

        [Space(20)]
        [Header(Fades)] [Space]
        _FadeBottomMin ("Fade Bottom Min", float) = -4
        _FadeBottomMax ("Fade Bottom Max", float) = 4
        _RunwayFadeOffset ("Runway Fade Offset", float) = -1
        _RunwayFadeScale ("Runway Fade Scale", float) = 10

        [Space(20)]
        [Header(Vertex)] [Space]
        [ShowIfAny(_VERTEXMODE_ROTATELAYERS)] _RotateLayerSpeeds ("Rotate Layer Speeds", Vector) = (16, 6, 2, 32)
        _VertexWaveFrequency ("Vertex Wave Frequency", float) = 4
        _VertexWaveAmplitude ("Vertex Wave Amplitude", float) = 0.03

        [Space(20)]
        [HideInInspector] _EnableFog ("Enable Fog (Compatibility Only)", float) = 1
        [HideInInspector] _FogStartOffset ("Fog Start Offset (Compatibility Only)", float) = 0
        [HideInInspector] _FogScale ("Fog Scale (Compatibility Only)", float) = 1
        [HideInInspector] _EnableHeightFog ("Enable Height Fog (Compatibility Only)", float) = 0

        [Space(20)]
        [Header(Features)] [Space]
        [Toggle(ALIGN_NORMALS_TO_WORLD_ORIGIN)] _AlignNormalsToWorldOrigin ("Align Normals To World Origin", float) = 0
        [Toggle(BACK_LIGHTING)] _EnableBackLighting ("Back Lighting", float) = 0
        [Toggle(DIFFUSE)] _EnableDiffuse ("Diffuse Lighting", float) = 1
        [Toggle(DIFFUSE_TEXTURE)] _EnableDiffuseTexture ("Diffuse Texture", float) = 0
        [Toggle(DISTORT_TEXTURE)] _EnableDistortTexture ("Distort Texture", float) = 0
        [Toggle(FADE_BOTTOM)] _EnableBottomFade ("Bottom Fade", float) = 0
        [Toggle(FADE_RUNWAY)] _EnableFadeRunway ("Fade Runway", float) = 0
        [Toggle(VERTEX_WAVE)] _EnableVertexWave ("Vertex Wave", float) = 0
        [KeywordEnum(None, Color, Emission, MetalSmoothness, Special, ColorAsAlpha, RotateLayers)] _VertexMode ("Vertex Color Mode", float) = 0

        [Space(20)]
        [Header(Render State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src Factor", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst Factor", Float) = 10
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #pragma shader_feature_local ALIGN_NORMALS_TO_WORLD_ORIGIN
            #pragma shader_feature_local_fragment BACK_LIGHTING
            #pragma shader_feature_local_fragment DIFFUSE
            #pragma shader_feature_local_fragment DIFFUSE_TEXTURE
            #pragma shader_feature_local_fragment DISTORT_TEXTURE
            #pragma shader_feature_local_fragment FADE_BOTTOM
            #pragma shader_feature_local_fragment FADE_RUNWAY
            #pragma shader_feature_local_vertex VERTEX_WAVE
            #pragma shader_feature_local_vertex _VERTEXMODE_ROTATELAYERS
            #pragma multi_compile _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Common/Lighting.hlsl"
            #include "ShaderLibrary/Common/ObjectShared.hlsl"

            #include "ShaderLibrary/Core/Tonemapping.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 rotationWeights : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 nor : TEXCOORD2;
                float4 position : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _DiffuseTex;
            float4 _DiffuseTex_ST;
            sampler2D _DistortTex;
            float4 _DistortTex_ST;
            float4 _DistortTexSpeed;
            float _DistortAmount;
            float4 _TimeHelperOffset;
            float _BackLightingBoost;
            float _FadeBottomMin;
            float _FadeBottomMax;
            float _RunwayFadeOffset;
            float _RunwayFadeScale;
            float4 _RotateLayerSpeeds;
            float _VertexWaveFrequency;
            float _VertexWaveAmplitude;

            UNITY_INSTANCING_BUFFER_START(CloudProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(CloudProps)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 pos = wp;
                #if defined(_VERTEXMODE_ROTATELAYERS)
                float layerA = dot(v.rotationWeights, _RotateLayerSpeeds.xyz);
                float angle = layerA * (_Time.x + _TimeHelperOffset.x) * 0.01745329424738884;
                pos = RotateObjectPositionY(wp, angle);
                #endif

                // wave on the world Y; the phase is driven by the unrotated
                // world X and the time component (asm 28-30)
                #if defined(VERTEX_WAVE)
                pos.y += sin(wp.x * _VertexWaveFrequency + _Time.z + _TimeHelperOffset.z) * _VertexWaveAmplitude;
                #endif

                o.world = pos;

                // The game has separate aligned and non-aligned normal routes.
                #if defined(ALIGN_NORMALS_TO_WORLD_ORIGIN)
                #if defined(SHADER_STAGE_VERTEX) \
                    && defined(_VERTEXMODE_ROTATELAYERS) && defined(VERTEX_WAVE) \
                    && !defined(UNITY_PASS_META) \
                    && !defined(UNITY_STEREO_MULTIVIEW_ENABLED) \
                    && !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) \
                    && !defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION) \
                    && ((defined(STEREO_INSTANCING_ON) && defined(UNITY_STEREO_INSTANCING_ENABLED)) \
                        || (!defined(UNITY_SINGLE_PASS_STEREO) && !defined(STEREO_INSTANCING_ON) \
                            && !defined(UNITY_STEREO_INSTANCING_ENABLED)))
                float3 localNormal = normalize(mul((float3x3)unity_WorldToObject,
                                                   float3(-pos.x, 0.0, -pos.z)));
                #if defined(UNITY_ASSUME_UNIFORM_SCALING)
                float3 worldNormal = mul((float3x3)unity_ObjectToWorld, localNormal);
                #else
                float3 worldNormal = mul(localNormal, (float3x3)unity_WorldToObject);
                #endif
                o.nor = worldNormal * rsqrt(max(dot(worldNormal, worldNormal), asfloat(0x00800000u)));
                #else
                o.nor = -normalize(float3(pos.x, 0.0, pos.z));
                #endif
                #else
                o.nor = normalize(mul((float3x3)unity_WorldToObject,
                                      float3(-pos.x, 0.0, -pos.z)));
                #endif

                o.uv = v.uv;
                o.position = mul(unity_MatrixVP, float4(pos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 tint = UNITY_ACCESS_INSTANCED_PROP(CloudProps, _Color);
                float4 albedo = tint;

                // The authority contains the complete Billie feature bundle and
                // the generic color route. Partial bundles have no fragment row.
                #if defined(ALIGN_NORMALS_TO_WORLD_ORIGIN) && \
                    defined(BACK_LIGHTING) && defined(DIFFUSE) && \
                    defined(DIFFUSE_TEXTURE) && defined(DISTORT_TEXTURE) && \
                    defined(FADE_BOTTOM) && defined(FADE_RUNWAY)
                float cloudTime = _Time.x + _TimeHelperOffset.x;
                float2 uvD = i.uv * _DistortTex_ST.xy + _DistortTex_ST.zw;
                uvD += cloudTime * _DistortTexSpeed.xy * _DistortTex_ST.xy;
                float4 d = tex2D(_DistortTex, uvD);

                float2 uvMain = i.uv * _DiffuseTex_ST.xy + _DiffuseTex_ST.zw;
                uvMain += _DistortAmount * float2(d.r - uvMain.x, d.a - uvMain.y);
                float4 base = tex2D(_DiffuseTex, uvMain);

                // The fragment consumes the trusted lighting vector directly
                // and reads directional-light RGB without an alpha gate.
                float3 light = CalculateLightDiffuse(i.nor);
                light += CalculateLightDiffuse(-i.nor) *
                    (base.g * _BackLightingBoost);

                // bottom fade: saturate((world.y - min) / (max - min)) squared
                // (recovered 104-107: clamp then x*x, not smoothstep)
                float bottom;
                {
                    float fade = saturate(
                        (i.world.y - _FadeBottomMin) / (_FadeBottomMax - _FadeBottomMin));
                    bottom = fade * fade;
                }
                albedo = float4(
                    base.b * light * tint.rgb * bottom,
                    base.a * tint.a * bottom);
                #endif

                #if defined(ACES_TONE_MAPPING)
                albedo = ApplyAcesTonemapping(albedo);
                #endif

                #if defined(ALIGN_NORMALS_TO_WORLD_ORIGIN) && \
                    defined(BACK_LIGHTING) && defined(DIFFUSE) && \
                    defined(DIFFUSE_TEXTURE) && defined(DISTORT_TEXTURE) && \
                    defined(FADE_BOTTOM) && defined(FADE_RUNWAY)
                // The authority applies runway attenuation after ACES.
                {
                    float gate = i.world.z > 0.0 ? 1.0 : 0.0;
                    float3 distanceVector = float3(
                        i.world.x, i.world.y - 1.0, gate);
                    albedo.a *= 1.0 - saturate(
                        gate * _RunwayFadeScale / length(distanceVector) + _RunwayFadeOffset);
                }
                #endif
                return albedo;
            }
            ENDHLSL
        }
    }
}
