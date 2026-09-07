// Replacement for the Beat Saber game shader Custom/Parametric3SliceSprite.
Shader "ChroMapper/Parametric Slice Billboard"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // PSB1. The 1.44.3 Custom/Parametric3SliceSprite Properties block is
    //       authoritative. _Color, _SizeParams, and _AlphaWidth are instanced
    //       runtime inputs and therefore remain unexposed.
    // PSB2. The vertex splits the source at UV.y 0.1, 0.5, and 0.9, applies
    //       independent cap/body widths, extends cap geometry, and optionally
    //       rotates local XZ around object-space Y to face the active camera.
    // PSB3. Source alpha is the selected alpha width cubed times _Color.a.
    //       SQUARE_ALPHA squares it before world noise and squared texture alpha.
    // PSB4. Fog can run before or after texture/noise. Bloom fog divides its
    //       distance scale by pre-square source alpha. Non-bloom height fog uses
    //       the inverse cubic ramp. ENABLE_BLOOM_FOG maps to BLOOM_FOG.
    // PSB5. MainEffect white boost is disabled by MAIN_EFFECT_ENABLED; Always is
    //       not. ChroMapper maps the global route to POST_BLOOM.
    // PSB6. Noise dithering adds masked blue noise after bloom composition. Its
    //       screen position is offset per frame and by object translation.
    //       Output alpha is final source alpha times _BloomMultiplier.
    // PSB7. OVERDRAW_VIEW and inactive ENABLE_MAIN_EFFECT_WHITE_BOOST routes are
    //       intentionally omitted. Stage binaries cannot prove ShaderLab state.
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [Space] _CapUVSize ("Cap UV Size", Float) = 0.25
        _OffsetFactor ("Offset Factor", Float) = 0
        _OffsetUnits ("Offset Units", Float) = 0

        [Space(12)] [Toggle(FOG)] _EnableFog ("Enable Fog", Float) = 1
        [ToggleShowIfAny(HEIGHT_FOG, FOG)] _EnableHeightFog ("Enable Height Fog", Float) = 1
        [ShowIfAny(2, HEIGHT_FOG, FOG)] _FogHeightScale ("Fog Height Scale", Float) = 1
        [ShowIfAny(2, HEIGHT_FOG, FOG)] _FogHeightOffset ("Fog Height Offset", Float) = 0
        [ToggleShowIfAny(USE_FOG_FOR_LIGHTS, FOG)] _UseFogForLights ("Use Fog for Lights", Float) = 1
        [ShowIfAny(FOG)] _FogStartOffset ("Fog Start Offset", Float) = 1
        [ShowIfAny(FOG)] _FogScale ("Fog Scale", Float) = 1

        [Space(12)] [Toggle(WORLD_NOISE)] _EnableWorldNoise ("Enable World Noise", Float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScale ("World Noise Scale", Float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityOffset ("World Intensity Offset", Float) = 0
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseIntensityScale ("World Intenstity Scale", Float) = 1
        [ShowIfAny(WORLD_NOISE)] _WorldNoiseScrolling ("World Noise Scrolling", Vector) = (0,0,0,1)
        [ToggleShowIfAny(WORLD_NOISE_WARP, WORLD_NOISE)] _WorldNoiseSkew ("Noise Field Warp", float) = 0
        [InfoBox(Decrease noise pattern repetition by varying noise field with worldspace Z, WORLD_NOISE_WARP)] [ShowIfAny(WORLD_NOISE_WARP)] _NoiseWarpZoomStrength ("Noise Warp Zoom Strength", Float) = 0
        [ShowIfAny(WORLD_NOISE_WARP)] _NoiseWarpSkewStrength ("Noise Warp Skew Strength", Float) = 0

        [Space()] [Toggle(WORLD_SPACE_FADE)] _EnableWorldSpaceFade ("Enable World Space Fade", Float) = 0
        [ShowIfAny(WORLD_SPACE_FADE)] _WorldSpaceFadePos ("World Space Fade Position", Float) = 0
        [ShowIfAny(WORLD_SPACE_FADE)] _WorldSpaceFadeSlope ("World Space Fade Slope", Float) = 1
        [Space()] [Toggle(ALPHA_WIDTH_SCALE)] _EnableAlphaWidthScale ("Enable Alpha Width Scale", Float) = 0

        [Space(20)] [KeywordEnum(None, MainEffect, Always)] _WhiteBoostType ("White Boost", Float) = 0
        [ShowIfAny(_WHITEBOOSTTYPE_ALWAYS, _WHITEBOOSTTYPE_MAINEFFECT)] _BloomWhiteMultiplier ("White Boost Multiplier", Float) = 1
        _BloomMultiplier ("Bloom Multiplier", Float) = 1

        [Space] [Header(Other)] [Space] [Toggle(SQUARE_ALPHA)] _SquareAlpha ("Square Alpha", Float) = 0
        [Toggle(ANGLE_DISAPPEAR)] _EnableEmissionAngleDisappear ("Angle Disappear", Float) = 10
        [Toggle(NOISE_DITHERING)] _EnableNoiseDithering ("Noise Dithering", Float) = 0
        [Toggle(Y_AXIS_BILLBOARD)] _EnableYAxisBillboard ("Y Axis Billboard", Float) = 1

        [Space] [Header(Settings)] [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src Factor A", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst Factor A", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        [Space] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("CullMode", Float) = 0
        [Space]
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Space] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite Off
        Offset [_OffsetFactor], [_OffsetUnits]

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

            #pragma shader_feature_local ALPHA_WIDTH_SCALE
            #pragma shader_feature_local_fragment SQUARE_ALPHA
            #pragma shader_feature_local_fragment ANGLE_DISAPPEAR
            #pragma shader_feature_local Y_AXIS_BILLBOARD
            #pragma shader_feature_local_fragment _ _WHITEBOOSTTYPE_MAINEFFECT _WHITEBOOSTTYPE_ALWAYS
            // Global: the post-process bloom runs (mirrors the game's MAIN_EFFECT_ENABLED gate).
            #pragma multi_compile _ POST_BLOOM

            #pragma shader_feature_local_fragment FOG
            #pragma shader_feature_local_fragment HEIGHT_FOG
            #pragma shader_feature_local_fragment USE_FOG_FOR_LIGHTS
            #pragma shader_feature_local_fragment WORLD_NOISE
            #pragma shader_feature_local_fragment WORLD_SPACE_FADE
            #pragma shader_feature_local_fragment WORLD_NOISE_WARP
            #pragma shader_feature_local_fragment NOISE_DITHERING

            #pragma multi_compile_fragment _ BLOOM_FOG

            #include "UnityCG.cginc"
            #include "ShaderLibrary/Core/Camera.hlsl"
            #include "ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "ShaderLibrary/Common/Bloom.hlsl"
            #include "ShaderLibrary/Families/ParametricShared.hlsl"
            #include "ShaderLibrary/Common/PostProcess.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _CapUVSize; // Scalar ShaderLab property; adjusts only the cap UV's Y coordinate.

            float _BloomMultiplier;
            float _BloomWhiteMultiplier;

            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;
            sampler3D _CutoutTex;
            float _WorldNoiseScale;
            float _WorldNoiseIntensityOffset;
            float _WorldNoiseIntensityScale;
            float3 _WorldNoiseScrolling;
            float _WorldSpaceFadePos;
            float _WorldSpaceFadeSlope;
            float _NoiseWarpZoomStrength;
            float _NoiseWarpSkewStrength;
            float4 _TimeHelperOffset;
            sampler2D _GlobalBlueNoiseTex;
            float2 _GlobalBlueNoiseParams;
            float _GlobalRandomValue;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SizeParams)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlphaWidth)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD2;
                float4 noiseScreenPos : TEXCOORD3;
                float angleFade : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float SliceFogFactor(float3 worldPos, float alphaDivisor)
            {
                #if defined(HEIGHT_FOG)
                float heightFade = CalculateParametricHeightRamp(
                    worldPos.y, _FogHeightScale, _FogHeightOffset,
                    _CustomFogHeightFogHeight, _CustomFogHeightFogStartY);
                #else
                float heightFade = 1.0;
                #endif
                #if defined(BLOOM_FOG)
                // Game 56ab3279/1dbc59fc: heightFade * distanceInverse with the
                // pre-square source alpha as the fog-scale divisor.
                float distanceInverse = CalculateParametricDistanceTransmission(
                    worldPos, GetParametricCameraPosition(), _FogStartOffset, _FogScale,
                    alphaDivisor, _CustomFogOffset, _CustomFogAttenuation);
                return heightFade * distanceInverse;
                #else
                #if defined(HEIGHT_FOG)
                return 1.0 - heightFade;
                #else
                return 1.0;
                #endif
                #endif
            }

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 alphaWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _AlphaWidth);
                float4 sizeParams = UNITY_ACCESS_INSTANCED_PROP(Props, _SizeParams);

                // DXBC 19215f1: four source regions split at .1, .5, and .9.
                // Each band carries its own width: expandedWidths.x / alphaWidth.w
                // (end cap / end body) and alphaWidth.z / expandedWidths.y
                // (start body / start cap). The fragment divides uvX by uvScale,
                // so the horizontal sample coordinate always equals uv.x.
                float sourceX = i.vertex.x * sizeParams.x;
                float2 expandedWidths = max(
                    (alphaWidth.wz - alphaWidth.zw) * 1.3333 + alphaWidth.zw,
                    alphaWidth.wz * 0.01);
                float uvX;
                float uvScale;
                float bandWidth;
                if (i.uv.y > 0.9)
                {
                    uvX = i.uv.x * expandedWidths.x;
                    uvScale = expandedWidths.x;
                    bandWidth = expandedWidths.x;
                }
                else if (i.uv.y > 0.5)
                {
                    uvX = i.uv.x * alphaWidth.w;
                    uvScale = alphaWidth.w;
                    bandWidth = alphaWidth.w;
                }
                else if (i.uv.y > 0.1)
                {
                    uvX = i.uv.x * alphaWidth.z;
                    uvScale = alphaWidth.z;
                    bandWidth = alphaWidth.z;
                }
                else
                {
                    uvX = i.uv.x * expandedWidths.y;
                    uvScale = expandedWidths.y;
                    bandWidth = expandedWidths.y;
                }

                float localX = sourceX * bandWidth;

                #if defined(ALPHA_WIDTH_SCALE)
                float widthAlpha = saturate(max(alphaWidth.x, alphaWidth.y) * color.a);
                float widthScale = saturate((widthAlpha - 0.02) / max(widthAlpha, 1e-6));
                widthScale = widthScale * widthScale * (3.0 - (2.0 * widthScale));
                localX *= widthScale;
                #endif

                float capVertex = abs(i.uv.y - 0.5) >= 0.49 ? 1.0 : 0.0;
                float localY = (i.vertex.y - sizeParams.z) * sizeParams.y +
                    (capVertex ? (i.vertex.y - 0.5) * sizeParams.w : 0.0);
                float capDirection = (i.uv.y < 0.5 ? 1.0 : 0.0) - (i.uv.y > 0.5 ? 1.0 : 0.0);
                float adjustedUvY = i.uv.y + (capVertex ? 0.0 : (0.36 - _CapUVSize) * capDirection);

                float3 localPosition = float3(localX, localY, i.vertex.z);
                float3 cameraObject = mul(
                    unity_WorldToObject, float4(GetParametricCameraPosition(), 1.0)).xyz;
                #if defined(Y_AXIS_BILLBOARD)
                // DXBC a5941c6e: rotate source XZ in object space, then use the full object transform.
                float2 cameraXZ = normalize(cameraObject.xz);
                float2 selectedLocalXZ = float2(localX, i.vertex.z);
                float billboardZ = dot(float2(cameraXZ.x, -cameraXZ.y), selectedLocalXZ);
                float billboardX = dot(float2(-cameraXZ.y, -cameraXZ.x), selectedLocalXZ);
                localPosition.xz = float2(billboardX, billboardZ);
                #endif

                float3 worldPos = mul(unity_ObjectToWorld, float4(localPosition, 1.0)).xyz;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                o.uv = float4(uvX, adjustedUvY, uvScale,
                              localY > (0.5 - sizeParams.z) ? alphaWidth.y : alphaWidth.x);
                float4 screenPos = ComputeScreenPosCustom(o.vertex);
                o.noiseScreenPos = BuildNoiseScreenPosition(
                    screenPos, o.vertex, _GlobalBlueNoiseParams,
                    _GlobalRandomValue, unity_ObjectToWorld._m03_m13);

                float cameraHeight = cameraObject.y - localY;
                float cameraDistance = sqrt(dot(cameraObject.xz, cameraObject.xz) + (cameraHeight * cameraHeight));
                float angle = saturate(
                    (dot(cameraObject.xz / cameraDistance, normalize(cameraObject.xz)) - 0.05) * 2.2222223);
                o.angleFade = angle * angle * (3.0 - (2.0 * angle));

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                float safeUvScale = max(abs(i.uv.z), 1e-5);
                // DXBC 5550caa4: the fragment divides horizontal uv by uvScale (so
                // the sample x is the plain uv.x) but passes vertical uv through.
                float2 adjustedUv = float2(i.uv.x / safeUvScale, i.uv.y);
                float alpha = i.uv.w * i.uv.w * i.uv.w * color.a;

                #if defined(ANGLE_DISAPPEAR)
                alpha *= i.angleFade;
                #endif

                // Game 4c799b9d/56ab3279: the fog factor is computed from the
                // pre-square source alpha (its divisor) and multiplied in either
                // before or after the texture depending on USE_FOG_FOR_LIGHTS.
                float preFogAlpha = alpha;

                #if defined(USE_FOG_FOR_LIGHTS) && defined(FOG)
                alpha *= SliceFogFactor(i.worldPos, preFogAlpha);
                #endif

                #if defined(SQUARE_ALPHA)
                // DXBC 40dd4e0f squares the cubic alpha before texture alpha squared.
                alpha *= alpha;
                #endif

                #if defined(WORLD_NOISE)
                float noise = SampleParametricWorldNoise(
                    i.worldPos, _CutoutTex, _WorldNoiseScrolling,
                    _Time.x + _TimeHelperOffset.x, _WorldNoiseScale,
                    _WorldNoiseIntensityOffset, _WorldNoiseIntensityScale,
                    _WorldSpaceFadePos, _WorldSpaceFadeSlope,
                    _NoiseWarpZoomStrength, _NoiseWarpSkewStrength);
                alpha *= noise;
                #endif

                // DXBC 5550caa4: no _MainTex_ST transform is applied.
                float textureAlpha = tex2D(_MainTex, adjustedUv).a;
                alpha *= textureAlpha * textureAlpha;

                // Original D3D11 MainEffect billboard routes apply the interpolated
                // angle fade once to source alpha and once after texture alpha.
                #if defined(SHADER_API_D3D11) && defined(Y_AXIS_BILLBOARD) && \
                    defined(ANGLE_DISAPPEAR) && defined(_WHITEBOOSTTYPE_MAINEFFECT) && \
                    !defined(POST_BLOOM) && !defined(ALPHA_WIDTH_SCALE) && \
                    !defined(SQUARE_ALPHA) && !defined(FOG) && !defined(HEIGHT_FOG) && \
                    !defined(USE_FOG_FOR_LIGHTS) && !defined(WORLD_NOISE) && \
                    !defined(WORLD_SPACE_FADE) && !defined(WORLD_NOISE_WARP) && \
                    !defined(NOISE_DITHERING) && !defined(UNITY_SINGLE_PASS_STEREO) && \
                    !defined(STEREO_MULTIVIEW_ON) && !defined(STEREO_CUBEMAP_RENDER_ON) && \
                    !defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                alpha *= i.angleFade;
                #endif

                #if !defined(USE_FOG_FOR_LIGHTS) && defined(FOG)
                alpha *= SliceFogFactor(i.worldPos, preFogAlpha);
                #endif

                half3 rgb = color.rgb * alpha;
                #if (defined(_WHITEBOOSTTYPE_ALWAYS) || \
                     (defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM)))
                // DXBC b005f58e (game main-effect type) / fc38f93c (game Always type):
                // both white-boost types share the same quartic term
                // whiteBoost = (bloomValue² * W)² * _BaseColorBoost - _BaseColorBoostThreshold
                // added to the premultiplied color. The game compiles the boost out
                // of the main-effect type when MAIN_EFFECT_ENABLED is on (POST_BLOOM
                // in ChroMapper); the Always type keeps it in both states.
                rgb = CalculateBloomComposition(
                    color.rgb, alpha, alpha * alpha, _BloomWhiteMultiplier,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif

                #if defined(NOISE_DITHERING)
                // The recovered shader uses a binary alpha threshold here. On
                // ChroMapper's additive target, its fixed 1/255 contribution can
                // dominate very faint premultiplied pixels. Fade only that low-
                // alpha tail while retaining full dithering above ~3% opacity.
                float ditherMask = alpha >= 0.001 ? saturate(alpha * 32.0) : 0.0;
                {
                    float2 noiseUv = i.noiseScreenPos.xy / i.noiseScreenPos.ww;
                    float noise = tex2D(_GlobalBlueNoiseTex, noiseUv).r - 0.5;
                    rgb = rgb + noise.xxx * (1.0 / 255.0) * ditherMask;
                }
                #endif

                // DXBC 5550caa4 carries bloom in alpha and has no ACES transform.
                return half4(rgb, alpha * _BloomMultiplier);
            }
            ENDHLSL
        }
    }
}