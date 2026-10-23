Shader "ChroMapper/Object/NoteDecoration"
{
    Properties
    {
        [Toggle(ARROW_GLOW)] _ArrowGlow ("Transparent Decoration", Float) = 0
        _MainTex ("Decoration", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _ColorMultiplier ("Color Multiplier", Float) = 1
        _ArrowDissolve ("Arrow Dissolve", Float) = 1
        _Cutout ("Cutout", Float) = 0
        _CutoutTexOffset ("Cutout Offset", Vector) = (0, 0, 0, 0)
        _CutoutTexScale ("Cutout Scale", Float) = 2
        _BlendSrcFactor ("Source Blend", Float) = 1
        _BlendDstFactor ("Destination Blend", Float) = 0
        _BlendSrcFactorA ("Source Alpha Blend", Float) = 0
        _BlendDstFactorA ("Destination Alpha Blend", Float) = 0
        _CullMode ("Cull", Float) = 2
        _ZWrite ("Z Write", Float) = 1
        _FogStartOffset ("Fog Start", Float) = 100
        _FogScale ("Fog Scale", Float) = 1
        _AnimationSpawned ("Animation Spawned", Float) = 0
        _ObjectTime ("Object Time", Float) = 9999
        _Rotation ("Rotation", Float) = 0
        _OutlineWidth ("Outline Width", Float) = 0.05
        _OverNoteInterfaceColor ("Interface Color", Color) = (1, 1, 1, 0)
        _AlwaysTranslucent ("Always Translucent", Float) = 0
        _TranslucentAlpha ("Translucent Alpha", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]
        Cull [_CullMode]
        ZWrite [_ZWrite]
        ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local ARROW_GLOW
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            #pragma multi_compile_fragment _ BLOOM_FOG
            #pragma multi_compile _ POST_BLOOM
            #pragma multi_compile_fragment _ CM_PREVIEW_MODE
            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Camera.hlsl"
            #include "../ShaderLibrary/Common/ObjectShared.hlsl"
            #include "../ShaderLibrary/Families/BloomFogComposition.hlsl"
            #include "../ShaderLibrary/Common/Bloom.hlsl"
            #include "../ShaderLibrary/Cutout.hlsl"

            sampler2D _MainTex;
            sampler3D _CutoutTex;
            float _CutoutTexScale;
            float _OutlineWidth;
            float _FogStartOffset;
            float _FogScale;
            float4 _SongBpmTime;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GlowColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _ColorMultiplier)
                UNITY_DEFINE_INSTANCED_PROP(float, _ArrowDissolve)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _AnimationSpawned)
                UNITY_DEFINE_INSTANCED_PROP(float, _ObjectTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OverNoteInterfaceColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlwaysTranslucent)
                UNITY_DEFINE_INSTANCED_PROP(float, _TranslucentAlpha)
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
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
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
                o.uv = i.uv;
                o.worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.screenPos = ComputeScreenPosCustom(o.vertex);
                float rotation = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (UNITY_PI / 180.0);
                o.rotatedPos = CalculateRotatedObjectPosition(o.worldPos, float3(0, 0, -1), rotation,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _ObjectTime), _SongBpmTime.y);
                o.rotatedPos.z -= 1.0;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float animation = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimationSpawned);
                float translucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);
                float editorAlpha = animation < 1.0 && (translucent >= 1.0 || i.rotatedPos.w <= 0.0)
                    ? UNITY_ACCESS_INSTANCED_PROP(Props, _TranslucentAlpha) : 1.0;
                // The marker must preserve each layer's shape before bypassing editor dithering.
                #if defined(ARROW_GLOW)
                // The full transparent arrow fades separately from the white noise-cutout core.
                float alpha = tex2D(_MainTex, i.uv).a * UNITY_ACCESS_INSTANCED_PROP(Props, _ArrowDissolve);
                alpha *= saturate(alpha);
                #else
                float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 cutoutPosition = CalculateObjectSpaceCutoutPosition(i.worldPos, origin,
                    UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset).xyz, _CutoutTexScale);
                float cutoutDistance = tex3D(_CutoutTex, cutoutPosition).a -
                    1.1 * UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout) + 0.1;
                clip(cutoutDistance);
                #endif
                #if !defined(CM_PREVIEW_MODE)
                if (abs(i.rotatedPos.z) < _OutlineWidth)
                {
                    float4 interfaceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverNoteInterfaceColor);
                    #if defined(ARROW_GLOW)
                    interfaceColor = float4(interfaceColor.rgb * alpha, alpha);
                    #endif
                    return interfaceColor;
                }
                #endif
                ApplyObjectEditorDither(editorAlpha, i.screenPos);
                #if defined(ARROW_GLOW)
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _GlowColor);
                #if defined(BLOOM_FOG)
                alpha *= 1.0 - CalculateCustomFogFactor(distanceSquared(i.worldPos), _FogStartOffset, _FogScale);
                #endif
                return float4(color.rgb * alpha, alpha);
                #else
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                color.rgb *= UNITY_ACCESS_INSTANCED_PROP(Props, _ColorMultiplier);
                float edge = max(1.0 - round(abs(cutoutDistance) + 0.45), 0.0);
                float4 result = color + edge * float4(1, 0, 0, 1);
                #if defined(BLOOM_FOG)
                result = ApplyBloomFog(result, i.screenPos, i.worldPos, _FogStartOffset, _FogScale);
                #endif
                #if !defined(POST_BLOOM)
                result.rgb = CalculateBloomComposition(result.rgb, 1.0, result.a, 1.0,
                    _BaseColorBoost, _BaseColorBoostThreshold);
                #endif
                return result;
                #endif
            }
            ENDHLSL
        }
    }
}
