Shader "ChroMapper/TextMeshPro/Distance Field"
{

    Properties
    {
        _FaceTex ("Face Texture", 2D) = "white" {}
        _FaceUVSpeedX ("Face UV Speed X", Range(-5, 5)) = 0.0
        _FaceUVSpeedY ("Face UV Speed Y", Range(-5, 5)) = 0.0
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineTex ("Outline Texture", 2D) = "white" {}
        _OutlineUVSpeedX ("Outline UV Speed X", Range(-5, 5)) = 0.0
        _OutlineUVSpeedY ("Outline UV Speed Y", Range(-5, 5)) = 0.0
        _OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0

        _UnderlayColor ("Border Color", Color) = (0,0,0, 0.5)
        _UnderlayOffsetX ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness ("Border Softness", Range(0,1)) = 0

        _WeightNormal ("Weight Normal", float) = 0
        _WeightBold ("Weight Bold", float) = 0.5

        _ShaderFlags ("Flags", float) = 0
        _ScaleRatioA ("Scale RatioA", float) = 1
        _ScaleRatioB ("Scale RatioB", float) = 1
        _ScaleRatioC ("Scale RatioC", float) = 1

        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", float) = 512
        _TextureHeight ("Texture Height", float) = 512
        _GradientScale ("Gradient Scale", float) = 5.0
        _ScaleX ("Scale X", float) = 1.0
        _ScaleY ("Scale Y", float) = 1.0
        _PerspectiveFilter ("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness ("Sharpness", Range(-1,1)) = 0

        _VertexOffsetX ("Vertex OffsetX", float) = 0
        _VertexOffsetY ("Vertex OffsetY", float) = 0

        _MaskCoord ("Mask Coordinates", vector) = (0, 0, 32767, 32767)
        _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _MaskSoftnessX ("Mask SoftnessX", float) = 0
        _MaskSoftnessY ("Mask SoftnessY", float) = 0

        _StencilComp ("Stencil Comparison", float) = 8
        _Stencil ("Stencil ID", float) = 0
        _StencilOp ("Stencil Operation", float) = 0
        _StencilWriteMask ("Stencil Write Mask", float) = 255
        _StencilReadMask ("Stencil Read Mask", float) = 255

        _CullMode ("Cull Mode", float) = 0
        _ColorMask ("Color Mask", float) = 15
    }

    SubShader
    {
        // Node text previously rendered opaque (no Queue tag, no Blend, alpha forced to 0), so the
        // SDF coverage never reached the framebuffer: clip() became a binary per-pixel cut and the
        // premultiplied rgb faded to black instead of blending - jagged edges and uneven dark borders.
        // Transparent queue + alpha blending is what turns the distance-field coverage into real AA.
        Tags
        {
            "Queue"="Transparent-200"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite On
        Lighting Off
        Fog
        {
            Mode Off
        }
        ColorMask [_ColorMask]

        Blend One OneMinusSrcAlpha, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc"
            #include "Assets/TextMesh Pro/Shaders/TMPro.cginc"

            struct vertex_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 position : POSITION;
                float3 normal : NORMAL;
                half4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct pixel_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 position : SV_POSITION;
                half4 color : COLOR;
                float2 atlas : TEXCOORD0; // Atlas
                float4 param : TEXCOORD1; // alphaClip, scale, bias, weight
                float4 mask : TEXCOORD2; // Position in object space(xy), pixel Size(zw)
                float3 viewDir : TEXCOORD3;

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                float4 texcoord2 : TEXCOORD4; // u,v, scale, bias
                half4 underlayColor : COLOR1;
                #endif

                float4 textures : TEXCOORD5;
            };

            // Used by Unity internally to handle Texture Tiling and Offset.
            float4 _FaceTex_ST;
            float4 _OutlineTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            pixel_t VertShader(vertex_t input)
            {
                pixel_t output;

                UNITY_INITIALIZE_OUTPUT(pixel_t, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord0.w, 0);

                float4 vert = input.position;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;

                float4 vPosition = UnityObjectToClipPos(vert);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord0.w) * _GradientScale * (_Sharpness + 1);
                if (UNITY_MATRIX_P[3][3] == 0)
                    scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale,
                                 abs(dot(
                                     UnityObjectToWorldNormal(
                                         input.normal.xyz),
                                     normalize(WorldSpaceViewDir(vert)))));

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

                float bias = (.5 - weight) + (.5 / scale);

                float alphaClip = (1.0 - _OutlineWidth * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);

                alphaClip = alphaClip / 2.0 - (.5 / scale) - weight;

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                float4 underlayColor = _UnderlayColor;
                underlayColor.rgb *= underlayColor.a;

                float bScale = scale;
                bScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * bScale);
                float bBias = (0.5 - weight) * bScale - 0.5 - ((_UnderlayDilate * _ScaleRatioC) * 0.5 * bScale);

                float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                float2 bOffset = float2(x, y);
                #endif

                // Generate UV for the Masking Texture
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

                // Support for texture tiling and offset
                float2 textureUV = input.texcoord1;
                float2 faceUV = TRANSFORM_TEX(textureUV, _FaceTex);
                float2 outlineUV = TRANSFORM_TEX(textureUV, _OutlineTex);

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    input.color.rgb = UIGammaToLinear(input.color.rgb);
                }
                output.position = vPosition;
                output.color = input.color;
                output.atlas = input.texcoord0;
                output.param = float4(alphaClip, scale, bias, weight);
                const half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX),
                                                 max(_UIMaskSoftnessY, _MaskSoftnessY));
                output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw,
                                    0.25 / (0.25 * maskSoftness + pixelSize.xy));
                output.viewDir = mul((float3x3)_EnvMatrix,
                                     _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, vert).xyz);
                #if (UNDERLAY_ON || UNDERLAY_INNER)
                output.texcoord2 = float4(input.texcoord0 + bOffset, bScale, bBias);
                output.underlayColor = underlayColor;
                #endif
                output.textures = float4(faceUV, outlineUV);

                return output;
            }


            half4 PixShader(pixel_t input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float c = tex2D(_MainTex, input.atlas).a;

                #if !defined(UNDERLAY_ON)
                clip(c - input.param.x);
                #endif

                float scale = input.param.y;
                float bias = input.param.z;
                float weight = input.param.w;
                float sd = (bias - c) * scale;

                float outline = (_OutlineWidth * _ScaleRatioA) * scale;
                float softness = (_OutlineSoftness * _ScaleRatioA) * scale;

                half4 faceColor = _FaceColor;
                half4 outlineColor = _OutlineColor;

                faceColor.rgb *= input.color.rgb;

                faceColor *= tex2D(_FaceTex, input.textures.xy + float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.y);
                outlineColor *= tex2D(_OutlineTex,
                                      input.textures.zw + float2(
                                          _OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.y);

                faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);

                #if UNDERLAY_ON
                float d = tex2D(_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
                faceColor += input.underlayColor * saturate(d - input.texcoord2.w) * (1 - faceColor.a);
                #endif

                #if UNDERLAY_INNER
                float d = tex2D(_MainTex, input.texcoord2.xy).a * input.texcoord2.z;
                faceColor += input.underlayColor * (1 - saturate(d - input.texcoord2.w)) * saturate(1 - sd) * (1 -
                    faceColor.a);
                #endif

                // Alternative implementation to UnityGet2DClipping with support for softness.
                #if UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                faceColor *= m.x * m.y;
                #endif

                #if UNITY_UI_ALPHACLIP
                clip(faceColor.a - 0.001);
                #endif

                // Coverage must stay in faceColor.a for the blend above. The old `faceColor.a = 0`
                // discard is what produced the hard aliased glyph and outline edges.
                return faceColor * input.color.a;
            }
            ENDHLSL
        }
    }

    Fallback "TextMeshPro/Mobile/Distance Field"
    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}