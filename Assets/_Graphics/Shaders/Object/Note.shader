Shader "ChroMapper/Object/Note"
{
    // Properties are the authoritative NoteHD/NoteLW union plus ChroMapper adapter data.
    // _Color always uses Props. An MPB overrides the material fallback.
    // Omitted routes remain serialized but have no authoritative formula.
    // ChroMapper owns _ZTest. The union retains the documented property limits.
    Properties
    {
        _Smoothness ("Smoothness", Range(0, 1)) = 1
        _NoteSize ("NoteSize", Float) = 0.25

        _Color ("Color", Color) = (0, 0, 0, 0)
        _ColorMultiplier ("Color Multiplier", Range(0, 10)) = 1

        // NoteLW-only authoritative block.
        [Space(12)] [Toggle(FAKE_MIRROR_TRANSPARENCY)] _FakeMirrorTransparencyEnabled ("Fake Mirror Transparency", Float) = 0
        [ShowIfAny(FAKE_MIRROR_TRANSPARENCY)] _FakeMirrorTransparencyMultiplier ("Mirror Transparency Multiplier", Float) = 1

        [Space(12)] [Toggle(REFLECTION_MAP)] _EnableReflectionMap ("Enable Reflection Map", Float) = 1
        [ShowIfAny(REFLECTION_MAP)] _EnvironmentReflectionCube ("Environment Reflection", Cube) = "" {}

        [Space(12)] [KeywordEnum(None, Alpha, Color, Lerp)] _FogType ("Fog Type", Float) = 0
        [ShowIfAny(_FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _FogStartOffset ("Fog Start Offset", Float) = 0
        [ShowIfAny(_FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _FogScale ("Fog Scale", Range(0, 1)) = 1
        [ToggleShowIfAny(HEIGHT_FOG, _FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _EnableHeightFog ("Enable Height Fog", Float) = 0
        [ShowIfAny(HEIGHT_FOG, _FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _FogHeightScale ("Fog Height Scale", Range(0, 1)) = 1
        [ShowIfAny(HEIGHT_FOG, _FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _FogHeightOffset ("Fog Height Offset", Float) = 0
        [ToggleShowIfAny(PRECISE_FOG, _FOGTYPE_ALPHA, _FOGTYPE_LERP, _FOGTYPE_COLOR)] _PreciseFog ("High (Frag) Precision", Float) = 0

        [Space(12)] [Toggle(CUTOUT)] _EnableCutout ("Enable Cutout", Float) = 0
        [ShowIfAny(CUTOUT)] _Cutout("Cutout", Range(0, 1)) = 0.0
        [ShowIfAny(CUTOUT)] _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)
        // NoteHD-only authoritative property.
        [ShowIfAny(CUTOUT)] _CutoutTexScale ("Cutout Texture Scale", Float) = 1

        [Space(12)] [Toggle(PLANE_CUT)] _EnablePlaneCut ("Plane Cut", Float) = 0
        [ShowIfAny(PLANE_CUT)] _CutPlaneEdgeGlowWidth ("Plane Edge Glow Width", Float) = 0.01
        [ShowIfAny(PLANE_CUT)] _CutPlane ("Cut Plane", Vector) = (1, 0, 0, 0)

        [Space(12)] [Toggle(RIM_DIM)] _EnableRimDim ("Rim Dim", Float) = 0
        [ShowIfAny(RIM_DIM)] _RimScale ("Rim Scale", Range(0, 10)) = 1
        [ShowIfAny(RIM_DIM)] _RimOffset ("Rim Offset", Range(0, 10)) = 1
        [ShowIfAny(RIM_DIM)] _RimCameraDistanceOffset ("Rim Camera Distance Offset", Range(0, 10)) = 2
        [ShowIfAny(RIM_DIM)] _RimCameraDistanceScale ("Rim Camera Distance Scale", Range(0, 10)) = 0.3
        [ShowIfAny(RIM_DIM)] _RimDarkening ("Rim Darkening", Range(0, 1)) = 0

        // NoteHD-only authoritative property.
        [Space(12)] [Enum(None, 0, MainEffect, 1, Always, 2)] _WhiteBoostType ("White Boost", Float) = 0

        [Space(16)] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
        // NoteLW-only authoritative stencil block.
        [Space(16)] _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Space(16)] [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor ("Blend Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor ("Blend Dst", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactorA ("Blend Src Factor A", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactorA ("Blend Dst Factor A", Float) = 0
        [Space(12)] [Toggle] _ZWrite ("Z Write", Float) = 1

        // ChroMapper-owned MPB/adapter properties. These are not game properties.
        [Header(Editor)]
        // Preview rendering samples this registered texture. A declaration alone
        // does not provide Unity 6's material default-texture binding.
        _MainTex ("Albedo", 2D) = "white" {}
        // NoteContainer, ChainContainer, and ObjectAnimator.
        _AnimationSpawned("Animation Spawned", float) = 0
        _ObjectTime ("Object Time", float) = 9999
        _Rotation("Rotation", float) = 0
        // GLSGroupAppearanceSO and GLSEventAppearanceSO.
        _StrobeColor ("Strobe Color", Color) = (0, 0, 0, 0)
        [Toggle] _StrobeColorEnabled ("Strobe Color Enabled", Float) = 0
        // ChroMapper timeline-interface adapter.
        _OutlineWidth("Outline Width", float) = 0.05
        _OverNoteInterfaceColor("Over Note Interface Color", Color) = (1, 1, 1, 0)
        // Placement and passed-object controllers.
        [Toggle] _AlwaysTranslucent("Always Translucent", float) = 0
        _TranslucentAlpha("Translucent Alpha", float) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        // The recovered shader does not contain recoverable ShaderLab state. Keep the
        // opaque ChroMapper states from Note.shader as the target-side adapter.
        Cull [_CullMode]
        ZWrite [_ZWrite]
        Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]
        Stencil
        {
            Ref [_StencilRefValue] Comp [_StencilComp] Pass [_StencilPass]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // These are the canonical target declarations for the recovered source
            // families. Do not restore the source aliases here.
            #pragma shader_feature_local PLANE_CUT
            #pragma shader_feature_local ZWRITE
            #pragma shader_feature_local FAKE_MIRROR_TRANSPARENCY
            // These are global/overridable in the recovered HD family.
            #pragma multi_compile _ CUTOUT
            #pragma multi_compile _ REFLECTION_MAP
            #pragma multi_compile _ RIM_DIM
            #pragma multi_compile_fragment _ _WHITEBOOSTTYPE_MAINEFFECT
            #pragma multi_compile_fragment _ HEIGHT_FOG
            #pragma multi_compile_fragment _ _FOGTYPE_LERP
            // Global: the post-process bloom runs (mirrors the game's MAIN_EFFECT_ENABLED gate).
            #pragma multi_compile _ POST_BLOOM

            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING
            #pragma multi_compile_fragment _ CM_PREVIEW_MODE

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Camera.hlsl"
            #include "../ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "../ShaderLibrary/Common/Bloom.hlsl"
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"
            #include "../ShaderLibrary/Common/ObjectShared.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler3D _CutoutTex;
            samplerCUBE _EnvironmentReflectionCube;

            float _Smoothness;
            float _NoteSize;
            float _FakeMirrorTransparencyMultiplier;
            float _CutoutTexScale;
            float _CutPlaneEdgeGlowWidth;

            float _RimScale;
            float _RimOffset;
            float _RimCameraDistanceScale;
            float _RimCameraDistanceOffset;
            float _RimDarkening;

            float _OutlineWidth;
            float _FogStartOffset;
            float _FogScale;
            float _FogHeightOffset;
            float _FogHeightScale;

            float4 _SongBpmTime;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StrobeColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _StrobeColorEnabled)
                UNITY_DEFINE_INSTANCED_PROP(float, _ColorMultiplier)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OverNoteInterfaceColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _TranslucentAlpha)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutPlane)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlwaysTranslucent)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationSpawned)
                UNITY_DEFINE_INSTANCED_PROP(float, _ObjectTime)
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 localPos : TEXCOORD1;
                float4 reflectionData : TEXCOORD2;
                float4 worldPos : TEXCOORD3;
                float4 rotatedPos : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 AdaptEnvironmentReflectionDirection(float3 worldReflectionDirection)
            {
                // Source variants apply the _SpawnRotation basis before the cube
                // lookup. ChroMapper supplies the equivalent Y rotation per note.
                float rotation = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180.0);
                return RotateObjectPositionY(worldReflectionDirection, rotation);
            }

            v2f vert(appdata i)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 lightingPosition = i.vertex;
                float4 clipPosition = i.vertex;
                #if defined(FAKE_MIRROR_TRANSPARENCY)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                #if defined(PLANE_CUT)
                // Project the LW lighting surface first; collapse only clip geometry.
                float4 cutPlane = UNITY_ACCESS_INSTANCED_PROP(Props, _CutPlane);
                float planeDistance = dot(i.vertex.xyz, cutPlane.xyz) + cutPlane.w;
                planeDistance -= cutout * (_NoteSize + cutPlane.w);
                lightingPosition.xyz -= min(planeDistance, 0.0) * cutPlane.xyz;
                float planeCollapse = saturate((cutout - 0.5) * 2.0);
                planeCollapse = planeCollapse * planeCollapse * (3.0 - 2.0 * planeCollapse);
                clipPosition.xyz = (1.0 - planeCollapse) * lightingPosition.xyz +
                    planeCollapse * cutPlane.xyz * _NoteSize;
                #else
                // LW cutout shrinks geometry, not the position used for lighting.
                clipPosition.xyz *= 1.0 - cutout;
                #endif
                #endif

                float3 cameraPosition = GetStereoAwareCameraPosition();
                float3 worldPosition = mul(unity_ObjectToWorld, lightingPosition).xyz;
                float3 worldNormal = normalize(UnityObjectToWorldNormal(i.normal));
                float3 V = normalize(worldPosition - cameraPosition);
                float distanceToCamera = length(worldPosition - cameraPosition);

                // This is the recovered sign convention: V points from the camera
                // toward the fragment, not from the fragment toward the camera.
                float3 worldReflectionDirection = V - 2.0 * dot(V, worldNormal) * worldNormal;
                float rimFactor = 0.0;
                #if defined(RIM_DIM)
                rimFactor = saturate((dot(V, worldNormal) + _RimOffset + 1.0) * _RimScale +
                    max(distanceToCamera - _RimCameraDistanceOffset, 0.0) *
                    _RimCameraDistanceScale);
                #endif

                o.vertex = UnityObjectToClipPos(clipPosition);
                o.uv = i.uv;
                o.localPos = lightingPosition;
                o.worldPos = float4(worldPosition, distanceToCamera);
                o.reflectionData = float4(
                    AdaptEnvironmentReflectionDirection(worldReflectionDirection), rimFactor);
                o.screenPos = ComputeScreenPosCustom(o.vertex);

                // These varyings retain the ChroMapper editor animation/translucency
                // contract. They do not participate in the recovered source lighting.
                const float4 offset = float4(0, 0, -1, 0);
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180.0);
                float objectTime = UNITY_ACCESS_INSTANCED_PROP(Props, _ObjectTime);
                o.rotatedPos = CalculateRotatedObjectPosition(
                    worldPosition, offset.xyz, rotationInRadians, objectTime, _SongBpmTime.y);
                o.rotatedPos.z -= 1.0;

                return o;
            }

            float4 frag(v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float4 strobeColor = UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColor);
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _StrobeColorEnabled) > 0.5)
                {
                    // The editor selects strobe RGB only. Strobe alpha does not enter output.
                    float splitCoordinate = abs(i.localPos.x + i.localPos.y + i.localPos.z) - 0.5;
                    if (splitCoordinate > 0.0)
                        color.rgb = strobeColor.rgb;
                }
                // Native _FinalColorMul maps to this editor MPB. Preserve its base/strobe
                // RGB contract, including HD edge brightness; LW also scales alpha below.
                float colorMultiplier = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorMultiplier);
                color.rgb *= colorMultiplier;
                float4 interfaceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverNoteInterfaceColor);
                float isTranslucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);
                float animation = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationSpawned);
                float translucentAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _TranslucentAlpha);
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);

                // Preview texturing follows face selection and _ColorMultiplier.
                color.rgb *= tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).rgb;

                float cutoutMask = 0.0;
                #if defined(FAKE_MIRROR_TRANSPARENCY)
                float planeColorMultiplier = 1.0;
                #endif

                #if defined(CUTOUT) && !defined(FAKE_MIRROR_TRANSPARENCY)
                float3 objectOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 cutoutPosition = (i.worldPos.xyz - objectOrigin + cutoutTexOffset.xyz) *
                    _CutoutTexScale;
                float noiseA = tex3D(_CutoutTex, cutoutPosition).a;
                float cutoutSample = noiseA - 1.1 * cutout;
                float cutoutDistance = cutoutSample + 0.1;
                if (cutoutDistance < 0.0)
                    discard;
                cutoutMask = saturate(1.0 - round(cutoutSample + 0.55));
                #endif

                #if defined(PLANE_CUT)
                // Both families subtract the cutout-scaled threshold. HD discards
                // the cut side; LW dims it and moves it outside the white edge.
                float4 cutPlane = UNITY_ACCESS_INSTANCED_PROP(Props, _CutPlane);
                float planeDistance = dot(i.localPos.xyz, cutPlane.xyz) + cutPlane.w;
                float planeThreshold = cutout * (_NoteSize + cutPlane.w);
                planeDistance -= planeThreshold;
                #if defined(FAKE_MIRROR_TRANSPARENCY)
                planeColorMultiplier = planeDistance >= 0.001 ? 1.0 : 0.1;
                planeDistance += planeDistance <= 0.001 ? 1.0 : 0.0;
                #else
                if (planeDistance < 0.0)
                    discard;
                #endif

                float planeEdge = saturate(
                    (planeDistance - (_CutPlaneEdgeGlowWidth + 0.005)) /
                    (_CutPlaneEdgeGlowWidth - (_CutPlaneEdgeGlowWidth + 0.005)));
                planeEdge = planeEdge * planeEdge * (3.0 - 2.0 * planeEdge);
                cutoutMask = min(planeEdge + cutoutMask, 1.0);
                #endif

                #if !defined(CM_PREVIEW_MODE)
                // Apply the editor timeline marker before transparent dithering.
                // This changes RGB only, so source reflection and cutout alpha remain intact.
                if (abs(i.rotatedPos.z) < _OutlineWidth)
                    return interfaceColor;
                color.rgb = abs(i.rotatedPos.z) < _OutlineWidth && isTranslucent < 1.0
                                ? interfaceColor.rgb
                                : color.rgb;
                #endif

                float editorAlpha = animation < 1.0 &&
                                    (isTranslucent >= 1.0 || i.rotatedPos.w <= 0.0)
                                        ? translucentAlpha
                                        : 1.0;
                // Dither is a ChroMapper editor adapter, after family-specific
                // cutout/plane evaluation.
                if (editorAlpha < 1.0)
                {
                    const float thresholds[16] =
                    {
                        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
                        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
                        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
                        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
                    };

                    float2 normalizedScreenPosition = i.screenPos.xy / i.screenPos.w;
                    int2 pixel = int2(normalizedScreenPosition * _ScreenParams.xy);
                    int index = (pixel.x & 3) * 4 + (pixel.y & 3);
                    float dither = editorAlpha - thresholds[index];
                    clip(dither);
                }

                float4 result = 0.0;
                float reflectionFactor = 1.0 - i.reflectionData.w * _RimDarkening;
                float reflectionFaceSign = 1.0;
                #if !defined(FAKE_MIRROR_TRANSPARENCY)
                // The standard route is HD. Only the existing mirror route selects LW.
                reflectionFactor *= isFrontFace ? 1.0 : 0.2592592537;
                reflectionFaceSign = isFrontFace ? 1.0 : -1.0;
                #endif
                float3 reflected = reflectionFactor * color.rgb;

                #if defined(REFLECTION_MAP)
                float3 environmentReflectionDirection =
                    reflectionFaceSign * i.reflectionData.xyz;
                float x = i.reflectionData.w + 1.0 - _Smoothness +
                    saturate(i.worldPos.w * 0.01 - 0.3);
                float reflectionLod = (1.7 - 0.7 * x) * x * 6.0;
                float4 environment = texCUBElod(
                    _EnvironmentReflectionCube,
                    float4(environmentReflectionDirection, reflectionLod));
                reflected *= environment.rgb;
                #endif

                #if defined(FAKE_MIRROR_TRANSPARENCY)
                // LW scales RGBA by rim and mapped color gain before premultiplication.
                result.a = color.a * reflectionFactor * colorMultiplier;
                result.rgb = reflected * result.a;
                #if defined(PLANE_CUT)
                float3 planeBaseMix = 1.0.xxx - reflected * result.a;
                result.rgb = planeColorMultiplier * (cutoutMask * planeBaseMix + result.rgb);
                #endif
                #else
                #if defined(REFLECTION_MAP)
                float3 weightedReflection = reflected * color.a;
                float3 baseMix = color.rgb - weightedReflection;
                result.rgb = cutoutMask * baseMix + weightedReflection;
                #else
                result.rgb = reflected * color.a;
                #endif
                // HD alpha is the deferred bloom/cut-edge mask.
                result.a = cutoutMask;
                #endif

                #if defined(ACES_TONE_MAPPING)
                result = ApplyAcesTonemapping(result);
                #endif

                #if defined(_WHITEBOOSTTYPE_MAINEFFECT) && !defined(POST_BLOOM) && !defined(FAKE_MIRROR_TRANSPARENCY)
                result.rgb = CalculateBloomComposition(
                    result.rgb, result.a, result.a, 1.0,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif

                float distanceSquared = i.worldPos.w * i.worldPos.w;
                #if defined(BLOOM_FOG) && defined(_FOGTYPE_LERP)
                float fogTransmission = 1.0 - CalculateCustomFogFactor(
                    distanceSquared, _FogStartOffset, _FogScale);
                float fogBlend = 1.0 - fogTransmission;
                #if defined(HEIGHT_FOG)
                float heightFogDensity = CalculateCustomHeightFogFactor(
                    i.worldPos.xyz, _FogHeightOffset, _FogHeightScale);
                // This ordering matches the recovered full fog program:
                // height density is multiplied by distance transmission before
                // the bloom prepass blend is evaluated.
                fogBlend = 1.0 - heightFogDensity * fogTransmission;
                #endif

                float4 bloomPrepassCol = SampleBloomPrePass(i.screenPos);
                float4 bloomfogCol = fogBlend * (-result + bloomPrepassCol) + result;
                float sourceAlpha = result.a;
                result = BlendFogColor(result, bloomfogCol);
                result.a = sourceAlpha;
                #elif defined(HEIGHT_FOG) && defined(_FOGTYPE_LERP)
                // The recovered no-bloom height route has no texture dependency.
                // It fades toward the observed constant height-fog color.
                float heightFogAmount = 1.0 - CalculateCustomHeightFogFactor(
                    i.worldPos.xyz, _FogHeightOffset, _FogHeightScale);
                result.rgb = lerp(result.rgb, 0.1.xxx, heightFogAmount);
                #endif

                #if defined(FAKE_MIRROR_TRANSPARENCY)
                // Preserve LW color-derived alpha through ACES/fog, then premultiply.
                // Native _FakeMirrorTransparency is fixed at 1 by this editor adapter.
                result.a *= _FakeMirrorTransparencyMultiplier;
                result.rgb *= result.a;
                #endif

                return result;
            }
            ENDHLSL
        }
    }
}