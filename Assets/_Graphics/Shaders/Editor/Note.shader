Shader "Custom/Note"
{
    Properties
    {
        _OutlineWidth("OutlineWidth", Float) = 0.05
        [ToggleUI]_AlwaysTranslucent("AlwaysTranslucent", Float) = 0
        _TranslucentAlpha("TranslucentAlpha", Float) = 0.5
        _OpaqueAlpha("OpaqueAlpha", Float) = 1
        _Color("NoteColor", Color) = (0, 0, 0, 0)
        _OverNoteInterfaceColor("OverNoteInterfaceColor", Color) = (1, 1, 1, 0)
        _Rotation("Rotation", Float) = 0
        _RotationAxis("Rotation Axis", Vector) = (0, 1, 0, 0)
        [ToggleUI]_Editor_IsPlaying("Editor_IsPlaying", Float) = 0
        [ToggleUI]_Lit("Lit", Float) = 1
    }
        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+0"
        }

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

        // Render State
        Blend One Zero, One Zero
        Cull Back
        ZTest LEqual
        ZWrite On
        // ColorMask: <None>


        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        // Debug
        // <None>

        // --------------------------------------------------
        // Pass

        // Pragmas
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #pragma target 2.0
        #pragma multi_compile_fog
        #pragma multi_compile_instancing

        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
        #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        // GraphKeywords: <None>

        // Defines
        #define _AlphaClip 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_POSITION_WS 
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define SHADERPASS_FORWARD

        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

        // --------------------------------------------------
        // Graph

        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _AlwaysTranslucent;
        float3 _RotationAxis;
        float _Editor_IsPlaying;
        float _Lit;
        #ifdef UNITY_DOTS_INSTANCING_ENABLED
        float _OutlineWidth_dummy;
        float _TranslucentAlpha_dummy;
        float _OpaqueAlpha_dummy;
        float4 _Color_dummy;
        float4 _OverNoteInterfaceColor_dummy;
        float _Rotation_dummy;
        #else
        float _OutlineWidth;
        float _TranslucentAlpha;
        float _OpaqueAlpha;
        float4 _Color;
        float4 _OverNoteInterfaceColor;
        float _Rotation;
        #endif
        CBUFFER_END

            // Graph Functions

            void Unity_Not_float(float In, out float Out)
            {
                Out = !In;
            }

            void Unity_Subtract_float(float A, float B, out float Out)
            {
                Out = A - B;
            }

            void Unity_DegreesToRadians_float(float In, out float Out)
            {
                Out = radians(In);
            }

            void Unity_Cosine_float(float In, out float Out)
            {
                Out = cos(In);
            }

            void Unity_Multiply_float(float A, float B, out float Out)
            {
                Out = A * B;
            }

            void Unity_Sine_float(float In, out float Out)
            {
                Out = sin(In);
            }

            void Unity_Add_float(float A, float B, out float Out)
            {
                Out = A + B;
            }

            struct Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b
            {
            };

            void SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(float3 Vector3_73761834, float3 Vector3_BF93F559, float Vector1_484A216, Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b IN, out float3 OutVector3_1)
            {
                float3 _Property_345E6F87_Out_0 = Vector3_73761834;
                float _Split_A3289C14_R_1 = _Property_345E6F87_Out_0[0];
                float _Split_A3289C14_G_2 = _Property_345E6F87_Out_0[1];
                float _Split_A3289C14_B_3 = _Property_345E6F87_Out_0[2];
                float _Split_A3289C14_A_4 = 0;
                float3 _Property_8E8D01D1_Out_0 = Vector3_BF93F559;
                float _Split_2E7606D_R_1 = _Property_8E8D01D1_Out_0[0];
                float _Split_2E7606D_G_2 = _Property_8E8D01D1_Out_0[1];
                float _Split_2E7606D_B_3 = _Property_8E8D01D1_Out_0[2];
                float _Split_2E7606D_A_4 = 0;
                float _Subtract_E7BBE4F_Out_2;
                Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_E7BBE4F_Out_2);
                float _Property_513AEC1A_Out_0 = Vector1_484A216;
                float _DegreesToRadians_6E84C064_Out_1;
                Unity_DegreesToRadians_float(_Property_513AEC1A_Out_0, _DegreesToRadians_6E84C064_Out_1);
                float _Cosine_E2EE9560_Out_1;
                Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_E2EE9560_Out_1);
                float _Multiply_3F455942_Out_2;
                Unity_Multiply_float(_Subtract_E7BBE4F_Out_2, _Cosine_E2EE9560_Out_1, _Multiply_3F455942_Out_2);
                float _Sine_E20E60B6_Out_1;
                Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_E20E60B6_Out_1);
                float _Subtract_D0AC5623_Out_2;
                Unity_Subtract_float(_Split_2E7606D_B_3, _Split_A3289C14_B_3, _Subtract_D0AC5623_Out_2);
                float _Multiply_A4C150E8_Out_2;
                Unity_Multiply_float(_Sine_E20E60B6_Out_1, _Subtract_D0AC5623_Out_2, _Multiply_A4C150E8_Out_2);
                float _Subtract_DCFFB660_Out_2;
                Unity_Subtract_float(_Multiply_3F455942_Out_2, _Multiply_A4C150E8_Out_2, _Subtract_DCFFB660_Out_2);
                float _Add_AFEED537_Out_2;
                Unity_Add_float(_Subtract_DCFFB660_Out_2, _Split_2E7606D_R_1, _Add_AFEED537_Out_2);
                float _Subtract_EE70C677_Out_2;
                Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_EE70C677_Out_2);
                float _Sine_741D7AFD_Out_1;
                Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_741D7AFD_Out_1);
                float _Multiply_4E6560B8_Out_2;
                Unity_Multiply_float(_Subtract_EE70C677_Out_2, _Sine_741D7AFD_Out_1, _Multiply_4E6560B8_Out_2);
                float _Cosine_1CE8158F_Out_1;
                Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_1CE8158F_Out_1);
                float _Subtract_79877950_Out_2;
                Unity_Subtract_float(_Split_A3289C14_B_3, _Split_2E7606D_B_3, _Subtract_79877950_Out_2);
                float _Multiply_F59A2E6D_Out_2;
                Unity_Multiply_float(_Cosine_1CE8158F_Out_1, _Subtract_79877950_Out_2, _Multiply_F59A2E6D_Out_2);
                float _Add_70355923_Out_2;
                Unity_Add_float(_Multiply_4E6560B8_Out_2, _Multiply_F59A2E6D_Out_2, _Add_70355923_Out_2);
                float _Add_F983B3DB_Out_2;
                Unity_Add_float(_Add_70355923_Out_2, _Split_2E7606D_B_3, _Add_F983B3DB_Out_2);
                float3 _Vector3_829B70A3_Out_0 = float3(_Add_AFEED537_Out_2, _Split_A3289C14_G_2, _Add_F983B3DB_Out_2);
                OutVector3_1 = _Vector3_829B70A3_Out_0;
            }

            void Unity_Absolute_float(float In, out float Out)
            {
                Out = abs(In);
            }

            void Unity_Comparison_Less_float(float A, float B, out float Out)
            {
                Out = A < B ? 1 : 0;
            }

            void Unity_And_float(float A, float B, out float Out)
            {
                Out = A && B;
            }

            void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
            {
                Out = Predicate ? True : False;
            }

            void Unity_Branch_float(float Predicate, float True, float False, out float Out)
            {
                Out = Predicate ? True : False;
            }

            void Unity_OneMinus_float(float In, out float Out)
            {
                Out = 1 - In;
            }

            void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
            {
                float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                Out = In - DITHER_THRESHOLDS[index];
            }

            // Graph Vertex
            // GraphVertex: <None>

            // Graph Pixel
            struct SurfaceDescriptionInputs
            {
                float3 TangentSpaceNormal;
                float3 WorldSpacePosition;
                float3 AbsoluteWorldSpacePosition;
                float4 ScreenPosition;
            };

            struct SurfaceDescription
            {
                float3 Albedo;
                float3 Normal;
                float3 Emission;
                float Metallic;
                float Smoothness;
                float Occlusion;
                float Alpha;
                float AlphaClipThreshold;
            };

            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
                float _Property_5BD30BFD_Out_0 = _AlwaysTranslucent;
                float _Not_8404A126_Out_1;
                Unity_Not_float(_Property_5BD30BFD_Out_0, _Not_8404A126_Out_1);
                float3 _Property_3A9DCF63_Out_0 = _RotationAxis;
                float _Property_C58E76FB_Out_0 = _Rotation;
                Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b _NoteRotation_39534DD6;
                float3 _NoteRotation_39534DD6_OutVector3_1;
                SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(IN.AbsoluteWorldSpacePosition, _Property_3A9DCF63_Out_0, _Property_C58E76FB_Out_0, _NoteRotation_39534DD6, _NoteRotation_39534DD6_OutVector3_1);
                float _Split_880CB10_R_1 = _NoteRotation_39534DD6_OutVector3_1[0];
                float _Split_880CB10_G_2 = _NoteRotation_39534DD6_OutVector3_1[1];
                float _Split_880CB10_B_3 = _NoteRotation_39534DD6_OutVector3_1[2];
                float _Split_880CB10_A_4 = 0;
                float _Absolute_36A33E1A_Out_1;
                Unity_Absolute_float(_Split_880CB10_B_3, _Absolute_36A33E1A_Out_1);
                float _Property_7755A765_Out_0 = _OutlineWidth;
                float _Comparison_6DAA9116_Out_2;
                Unity_Comparison_Less_float(_Absolute_36A33E1A_Out_1, _Property_7755A765_Out_0, _Comparison_6DAA9116_Out_2);
                float _And_1E7C4EAD_Out_2;
                Unity_And_float(_Not_8404A126_Out_1, _Comparison_6DAA9116_Out_2, _And_1E7C4EAD_Out_2);
                float4 _Property_D57744B8_Out_0 = _OverNoteInterfaceColor;
                float4 _Property_41A3C666_Out_0 = _Color;
                float4 _Branch_BBAFE61D_Out_3;
                Unity_Branch_float4(_And_1E7C4EAD_Out_2, _Property_D57744B8_Out_0, _Property_41A3C666_Out_0, _Branch_BBAFE61D_Out_3);
                float _Property_CBF4C1D0_Out_0 = _Lit;
                float _Branch_9DC62700_Out_3;
                Unity_Branch_float(_Property_CBF4C1D0_Out_0, 0.5, 0, _Branch_9DC62700_Out_3);
                float _Branch_FC37A51C_Out_3;
                Unity_Branch_float(_Property_CBF4C1D0_Out_0, 0.7, 0, _Branch_FC37A51C_Out_3);
                float _Property_940D9AAE_Out_0 = _AlwaysTranslucent;
                float _Property_3FBBCA67_Out_0 = _TranslucentAlpha;
                float _Property_791A452F_Out_0 = _OpaqueAlpha;
                float _Branch_C1EA1E3_Out_3;
                Unity_Branch_float(_Property_940D9AAE_Out_0, _Property_3FBBCA67_Out_0, _Property_791A452F_Out_0, _Branch_C1EA1E3_Out_3);
                float _OneMinus_5E47A0FD_Out_1;
                Unity_OneMinus_float(_Branch_C1EA1E3_Out_3, _OneMinus_5E47A0FD_Out_1);
                float _Dither_65B0625B_Out_2;
                Unity_Dither_float(_OneMinus_5E47A0FD_Out_1, float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _Dither_65B0625B_Out_2);
                surface.Albedo = (_Branch_BBAFE61D_Out_3.xyz);
                surface.Normal = IN.TangentSpaceNormal;
                surface.Emission = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
                surface.Metallic = _Branch_9DC62700_Out_3;
                surface.Smoothness = _Branch_FC37A51C_Out_3;
                surface.Occlusion = 1;
                surface.Alpha = 0;
                surface.AlphaClipThreshold = _Dither_65B0625B_Out_2;
                return surface;
            }

            // --------------------------------------------------
            // Structs and Packing

            // Generated Type: Attributes
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv1 : TEXCOORD1;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            // Generated Type: Varyings
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS;
                float3 normalWS;
                float4 tangentWS;
                float3 viewDirectionWS;
                #if defined(LIGHTMAP_ON)
                float2 lightmapUV;
                #endif
                #if !defined(LIGHTMAP_ON)
                float3 sh;
                #endif
                float4 fogFactorAndVertexLight;
                float4 shadowCoord;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };

            // Generated Type: PackedVaryings
            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                #if defined(LIGHTMAP_ON)
                #endif
                #if !defined(LIGHTMAP_ON)
                #endif
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                float3 interp00 : TEXCOORD0;
                float3 interp01 : TEXCOORD1;
                float4 interp02 : TEXCOORD2;
                float3 interp03 : TEXCOORD3;
                float2 interp04 : TEXCOORD4;
                float3 interp05 : TEXCOORD5;
                float4 interp06 : TEXCOORD6;
                float4 interp07 : TEXCOORD7;
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };

            // Packed Type: Varyings
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionWS;
                output.interp01.xyz = input.normalWS;
                output.interp02.xyzw = input.tangentWS;
                output.interp03.xyz = input.viewDirectionWS;
                #if defined(LIGHTMAP_ON)
                output.interp04.xy = input.lightmapUV;
                #endif
                #if !defined(LIGHTMAP_ON)
                output.interp05.xyz = input.sh;
                #endif
                output.interp06.xyzw = input.fogFactorAndVertexLight;
                output.interp07.xyzw = input.shadowCoord;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }

            // Unpacked Type: Varyings
            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = input.positionCS;
                output.positionWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                output.tangentWS = input.interp02.xyzw;
                output.viewDirectionWS = input.interp03.xyz;
                #if defined(LIGHTMAP_ON)
                output.lightmapUV = input.interp04.xy;
                #endif
                #if !defined(LIGHTMAP_ON)
                output.sh = input.interp05.xyz;
                #endif
                output.fogFactorAndVertexLight = input.interp06.xyzw;
                output.shadowCoord = input.interp07.xyzw;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }

            // --------------------------------------------------
            // Build Graph Inputs

            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



                output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


                output.WorldSpacePosition = input.positionWS;
                output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);
                output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
            #else
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            #endif
            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

                return output;
            }


            // --------------------------------------------------
            // Main

            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

                // Render State
                Blend One Zero, One Zero
                Cull Back
                ZTest LEqual
                ZWrite On
                // ColorMask: <None>


                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                // Debug
                // <None>

                // --------------------------------------------------
                // Pass

                // Pragmas
                #pragma prefer_hlslcc gles
                #pragma exclude_renderers d3d11_9x
                #pragma target 2.0
                #pragma multi_compile_instancing

                // Keywords
                // PassKeywords: <None>
                // GraphKeywords: <None>

                // Defines
                #define _AlphaClip 1
                #define _NORMAL_DROPOFF_TS 1
                #define ATTRIBUTES_NEED_NORMAL
                #define ATTRIBUTES_NEED_TANGENT
                #define VARYINGS_NEED_POSITION_WS 
                #define SHADERPASS_SHADOWCASTER

                // Includes
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
                #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

                // --------------------------------------------------
                // Graph

                // Graph Properties
                CBUFFER_START(UnityPerMaterial)
                float _AlwaysTranslucent;
                float3 _RotationAxis;
                float _Editor_IsPlaying;
                float _Lit;
                #ifdef UNITY_DOTS_INSTANCING_ENABLED
                float _OutlineWidth_dummy;
                float _TranslucentAlpha_dummy;
                float _OpaqueAlpha_dummy;
                float4 _Color_dummy;
                float4 _OverNoteInterfaceColor_dummy;
                float _Rotation_dummy;
                #else
                float _OutlineWidth;
                float _TranslucentAlpha;
                float _OpaqueAlpha;
                float4 _Color;
                float4 _OverNoteInterfaceColor;
                float _Rotation;
                #endif
                CBUFFER_END

                    // Graph Functions

                    void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                    {
                        Out = Predicate ? True : False;
                    }

                    void Unity_OneMinus_float(float In, out float Out)
                    {
                        Out = 1 - In;
                    }

                    void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
                    {
                        float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                        float DITHER_THRESHOLDS[16] =
                        {
                            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                            13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                            4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                            16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                        };
                        uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                        Out = In - DITHER_THRESHOLDS[index];
                    }

                    // Graph Vertex
                    // GraphVertex: <None>

                    // Graph Pixel
                    struct SurfaceDescriptionInputs
                    {
                        float3 TangentSpaceNormal;
                        float3 WorldSpacePosition;
                        float4 ScreenPosition;
                    };

                    struct SurfaceDescription
                    {
                        float Alpha;
                        float AlphaClipThreshold;
                    };

                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                    {
                        SurfaceDescription surface = (SurfaceDescription)0;
                        float _Property_940D9AAE_Out_0 = _AlwaysTranslucent;
                        float _Property_3FBBCA67_Out_0 = _TranslucentAlpha;
                        float _Property_791A452F_Out_0 = _OpaqueAlpha;
                        float _Branch_C1EA1E3_Out_3;
                        Unity_Branch_float(_Property_940D9AAE_Out_0, _Property_3FBBCA67_Out_0, _Property_791A452F_Out_0, _Branch_C1EA1E3_Out_3);
                        float _OneMinus_5E47A0FD_Out_1;
                        Unity_OneMinus_float(_Branch_C1EA1E3_Out_3, _OneMinus_5E47A0FD_Out_1);
                        float _Dither_65B0625B_Out_2;
                        Unity_Dither_float(_OneMinus_5E47A0FD_Out_1, float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _Dither_65B0625B_Out_2);
                        surface.Alpha = 0;
                        surface.AlphaClipThreshold = _Dither_65B0625B_Out_2;
                        return surface;
                    }

                    // --------------------------------------------------
                    // Structs and Packing

                    // Generated Type: Attributes
                    struct Attributes
                    {
                        float3 positionOS : POSITION;
                        float3 normalOS : NORMAL;
                        float4 tangentOS : TANGENT;
                        #if UNITY_ANY_INSTANCING_ENABLED
                        uint instanceID : INSTANCEID_SEMANTIC;
                        #endif
                    };

                    // Generated Type: Varyings
                    struct Varyings
                    {
                        float4 positionCS : SV_POSITION;
                        float3 positionWS;
                        #if UNITY_ANY_INSTANCING_ENABLED
                        uint instanceID : CUSTOM_INSTANCE_ID;
                        #endif
                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                        uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                        #endif
                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                        uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                        #endif
                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                        #endif
                    };

                    // Generated Type: PackedVaryings
                    struct PackedVaryings
                    {
                        float4 positionCS : SV_POSITION;
                        #if UNITY_ANY_INSTANCING_ENABLED
                        uint instanceID : CUSTOM_INSTANCE_ID;
                        #endif
                        float3 interp00 : TEXCOORD0;
                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                        uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                        #endif
                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                        uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                        #endif
                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                        #endif
                    };

                    // Packed Type: Varyings
                    PackedVaryings PackVaryings(Varyings input)
                    {
                        PackedVaryings output = (PackedVaryings)0;
                        output.positionCS = input.positionCS;
                        output.interp00.xyz = input.positionWS;
                        #if UNITY_ANY_INSTANCING_ENABLED
                        output.instanceID = input.instanceID;
                        #endif
                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                        output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                        #endif
                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                        output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                        #endif
                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                        output.cullFace = input.cullFace;
                        #endif
                        return output;
                    }

                    // Unpacked Type: Varyings
                    Varyings UnpackVaryings(PackedVaryings input)
                    {
                        Varyings output = (Varyings)0;
                        output.positionCS = input.positionCS;
                        output.positionWS = input.interp00.xyz;
                        #if UNITY_ANY_INSTANCING_ENABLED
                        output.instanceID = input.instanceID;
                        #endif
                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                        output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                        #endif
                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                        output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                        #endif
                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                        output.cullFace = input.cullFace;
                        #endif
                        return output;
                    }

                    // --------------------------------------------------
                    // Build Graph Inputs

                    SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                    {
                        SurfaceDescriptionInputs output;
                        ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



                        output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


                        output.WorldSpacePosition = input.positionWS;
                        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                    #else
                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                    #endif
                    #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

                        return output;
                    }


                    // --------------------------------------------------
                    // Main

                    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
                    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

                    ENDHLSL
                }

                Pass
                {
                    Name "DepthOnly"
                    Tags
                    {
                        "LightMode" = "DepthOnly"
                    }

                        // Render State
                        Blend One Zero, One Zero
                        Cull Back
                        ZTest LEqual
                        ZWrite On
                        ColorMask 0


                        HLSLPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag

                        // Debug
                        // <None>

                        // --------------------------------------------------
                        // Pass

                        // Pragmas
                        #pragma prefer_hlslcc gles
                        #pragma exclude_renderers d3d11_9x
                        #pragma target 2.0
                        #pragma multi_compile_instancing

                        // Keywords
                        // PassKeywords: <None>
                        // GraphKeywords: <None>

                        // Defines
                        #define _AlphaClip 1
                        #define _NORMAL_DROPOFF_TS 1
                        #define ATTRIBUTES_NEED_NORMAL
                        #define ATTRIBUTES_NEED_TANGENT
                        #define VARYINGS_NEED_POSITION_WS 
                        #define SHADERPASS_DEPTHONLY

                        // Includes
                        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
                        #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

                        // --------------------------------------------------
                        // Graph

                        // Graph Properties
                        CBUFFER_START(UnityPerMaterial)
                        float _AlwaysTranslucent;
                        float3 _RotationAxis;
                        float _Editor_IsPlaying;
                        float _Lit;
                        #ifdef UNITY_DOTS_INSTANCING_ENABLED
                        float _OutlineWidth_dummy;
                        float _TranslucentAlpha_dummy;
                        float _OpaqueAlpha_dummy;
                        float4 _Color_dummy;
                        float4 _OverNoteInterfaceColor_dummy;
                        float _Rotation_dummy;
                        #else
                        float _OutlineWidth;
                        float _TranslucentAlpha;
                        float _OpaqueAlpha;
                        float4 _Color;
                        float4 _OverNoteInterfaceColor;
                        float _Rotation;
                        #endif
                        CBUFFER_END

                            // Graph Functions

                            void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                            {
                                Out = Predicate ? True : False;
                            }

                            void Unity_OneMinus_float(float In, out float Out)
                            {
                                Out = 1 - In;
                            }

                            void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
                            {
                                float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                                float DITHER_THRESHOLDS[16] =
                                {
                                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                                };
                                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                                Out = In - DITHER_THRESHOLDS[index];
                            }

                            // Graph Vertex
                            // GraphVertex: <None>

                            // Graph Pixel
                            struct SurfaceDescriptionInputs
                            {
                                float3 TangentSpaceNormal;
                                float3 WorldSpacePosition;
                                float4 ScreenPosition;
                            };

                            struct SurfaceDescription
                            {
                                float Alpha;
                                float AlphaClipThreshold;
                            };

                            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                            {
                                SurfaceDescription surface = (SurfaceDescription)0;
                                float _Property_940D9AAE_Out_0 = _AlwaysTranslucent;
                                float _Property_3FBBCA67_Out_0 = _TranslucentAlpha;
                                float _Property_791A452F_Out_0 = _OpaqueAlpha;
                                float _Branch_C1EA1E3_Out_3;
                                Unity_Branch_float(_Property_940D9AAE_Out_0, _Property_3FBBCA67_Out_0, _Property_791A452F_Out_0, _Branch_C1EA1E3_Out_3);
                                float _OneMinus_5E47A0FD_Out_1;
                                Unity_OneMinus_float(_Branch_C1EA1E3_Out_3, _OneMinus_5E47A0FD_Out_1);
                                float _Dither_65B0625B_Out_2;
                                Unity_Dither_float(_OneMinus_5E47A0FD_Out_1, float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _Dither_65B0625B_Out_2);
                                surface.Alpha = 0;
                                surface.AlphaClipThreshold = _Dither_65B0625B_Out_2;
                                return surface;
                            }

                            // --------------------------------------------------
                            // Structs and Packing

                            // Generated Type: Attributes
                            struct Attributes
                            {
                                float3 positionOS : POSITION;
                                float3 normalOS : NORMAL;
                                float4 tangentOS : TANGENT;
                                #if UNITY_ANY_INSTANCING_ENABLED
                                uint instanceID : INSTANCEID_SEMANTIC;
                                #endif
                            };

                            // Generated Type: Varyings
                            struct Varyings
                            {
                                float4 positionCS : SV_POSITION;
                                float3 positionWS;
                                #if UNITY_ANY_INSTANCING_ENABLED
                                uint instanceID : CUSTOM_INSTANCE_ID;
                                #endif
                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                #endif
                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                #endif
                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                #endif
                            };

                            // Generated Type: PackedVaryings
                            struct PackedVaryings
                            {
                                float4 positionCS : SV_POSITION;
                                #if UNITY_ANY_INSTANCING_ENABLED
                                uint instanceID : CUSTOM_INSTANCE_ID;
                                #endif
                                float3 interp00 : TEXCOORD0;
                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                #endif
                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                #endif
                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                #endif
                            };

                            // Packed Type: Varyings
                            PackedVaryings PackVaryings(Varyings input)
                            {
                                PackedVaryings output = (PackedVaryings)0;
                                output.positionCS = input.positionCS;
                                output.interp00.xyz = input.positionWS;
                                #if UNITY_ANY_INSTANCING_ENABLED
                                output.instanceID = input.instanceID;
                                #endif
                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                #endif
                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                #endif
                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                output.cullFace = input.cullFace;
                                #endif
                                return output;
                            }

                            // Unpacked Type: Varyings
                            Varyings UnpackVaryings(PackedVaryings input)
                            {
                                Varyings output = (Varyings)0;
                                output.positionCS = input.positionCS;
                                output.positionWS = input.interp00.xyz;
                                #if UNITY_ANY_INSTANCING_ENABLED
                                output.instanceID = input.instanceID;
                                #endif
                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                #endif
                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                #endif
                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                output.cullFace = input.cullFace;
                                #endif
                                return output;
                            }

                            // --------------------------------------------------
                            // Build Graph Inputs

                            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                            {
                                SurfaceDescriptionInputs output;
                                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



                                output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


                                output.WorldSpacePosition = input.positionWS;
                                output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
                            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                            #else
                            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                            #endif
                            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

                                return output;
                            }


                            // --------------------------------------------------
                            // Main

                            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
                            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

                            ENDHLSL
                        }

                        Pass
                        {
                            Name "Meta"
                            Tags
                            {
                                "LightMode" = "Meta"
                            }

                                // Render State
                                Blend One Zero, One Zero
                                Cull Back
                                ZTest LEqual
                                ZWrite On
                                // ColorMask: <None>


                                HLSLPROGRAM
                                #pragma vertex vert
                                #pragma fragment frag

                                // Debug
                                // <None>

                                // --------------------------------------------------
                                // Pass

                                // Pragmas
                                #pragma prefer_hlslcc gles
                                #pragma exclude_renderers d3d11_9x
                                #pragma target 2.0

                                // Keywords
                                #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
                                // GraphKeywords: <None>

                                // Defines
                                #define _AlphaClip 1
                                #define _NORMAL_DROPOFF_TS 1
                                #define ATTRIBUTES_NEED_NORMAL
                                #define ATTRIBUTES_NEED_TANGENT
                                #define ATTRIBUTES_NEED_TEXCOORD1
                                #define ATTRIBUTES_NEED_TEXCOORD2
                                #define VARYINGS_NEED_POSITION_WS 
                                #define SHADERPASS_META

                                // Includes
                                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
                                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
                                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
                                #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

                                // --------------------------------------------------
                                // Graph

                                // Graph Properties
                                CBUFFER_START(UnityPerMaterial)
                                float _AlwaysTranslucent;
                                float3 _RotationAxis;
                                float _Editor_IsPlaying;
                                float _Lit;
                                #ifdef UNITY_DOTS_INSTANCING_ENABLED
                                float _OutlineWidth_dummy;
                                float _TranslucentAlpha_dummy;
                                float _OpaqueAlpha_dummy;
                                float4 _Color_dummy;
                                float4 _OverNoteInterfaceColor_dummy;
                                float _Rotation_dummy;
                                #else
                                float _OutlineWidth;
                                float _TranslucentAlpha;
                                float _OpaqueAlpha;
                                float4 _Color;
                                float4 _OverNoteInterfaceColor;
                                float _Rotation;
                                #endif
                                CBUFFER_END

                                    // Graph Functions

                                    void Unity_Not_float(float In, out float Out)
                                    {
                                        Out = !In;
                                    }

                                    void Unity_Subtract_float(float A, float B, out float Out)
                                    {
                                        Out = A - B;
                                    }

                                    void Unity_DegreesToRadians_float(float In, out float Out)
                                    {
                                        Out = radians(In);
                                    }

                                    void Unity_Cosine_float(float In, out float Out)
                                    {
                                        Out = cos(In);
                                    }

                                    void Unity_Multiply_float(float A, float B, out float Out)
                                    {
                                        Out = A * B;
                                    }

                                    void Unity_Sine_float(float In, out float Out)
                                    {
                                        Out = sin(In);
                                    }

                                    void Unity_Add_float(float A, float B, out float Out)
                                    {
                                        Out = A + B;
                                    }

                                    struct Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b
                                    {
                                    };

                                    void SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(float3 Vector3_73761834, float3 Vector3_BF93F559, float Vector1_484A216, Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b IN, out float3 OutVector3_1)
                                    {
                                        float3 _Property_345E6F87_Out_0 = Vector3_73761834;
                                        float _Split_A3289C14_R_1 = _Property_345E6F87_Out_0[0];
                                        float _Split_A3289C14_G_2 = _Property_345E6F87_Out_0[1];
                                        float _Split_A3289C14_B_3 = _Property_345E6F87_Out_0[2];
                                        float _Split_A3289C14_A_4 = 0;
                                        float3 _Property_8E8D01D1_Out_0 = Vector3_BF93F559;
                                        float _Split_2E7606D_R_1 = _Property_8E8D01D1_Out_0[0];
                                        float _Split_2E7606D_G_2 = _Property_8E8D01D1_Out_0[1];
                                        float _Split_2E7606D_B_3 = _Property_8E8D01D1_Out_0[2];
                                        float _Split_2E7606D_A_4 = 0;
                                        float _Subtract_E7BBE4F_Out_2;
                                        Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_E7BBE4F_Out_2);
                                        float _Property_513AEC1A_Out_0 = Vector1_484A216;
                                        float _DegreesToRadians_6E84C064_Out_1;
                                        Unity_DegreesToRadians_float(_Property_513AEC1A_Out_0, _DegreesToRadians_6E84C064_Out_1);
                                        float _Cosine_E2EE9560_Out_1;
                                        Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_E2EE9560_Out_1);
                                        float _Multiply_3F455942_Out_2;
                                        Unity_Multiply_float(_Subtract_E7BBE4F_Out_2, _Cosine_E2EE9560_Out_1, _Multiply_3F455942_Out_2);
                                        float _Sine_E20E60B6_Out_1;
                                        Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_E20E60B6_Out_1);
                                        float _Subtract_D0AC5623_Out_2;
                                        Unity_Subtract_float(_Split_2E7606D_B_3, _Split_A3289C14_B_3, _Subtract_D0AC5623_Out_2);
                                        float _Multiply_A4C150E8_Out_2;
                                        Unity_Multiply_float(_Sine_E20E60B6_Out_1, _Subtract_D0AC5623_Out_2, _Multiply_A4C150E8_Out_2);
                                        float _Subtract_DCFFB660_Out_2;
                                        Unity_Subtract_float(_Multiply_3F455942_Out_2, _Multiply_A4C150E8_Out_2, _Subtract_DCFFB660_Out_2);
                                        float _Add_AFEED537_Out_2;
                                        Unity_Add_float(_Subtract_DCFFB660_Out_2, _Split_2E7606D_R_1, _Add_AFEED537_Out_2);
                                        float _Subtract_EE70C677_Out_2;
                                        Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_EE70C677_Out_2);
                                        float _Sine_741D7AFD_Out_1;
                                        Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_741D7AFD_Out_1);
                                        float _Multiply_4E6560B8_Out_2;
                                        Unity_Multiply_float(_Subtract_EE70C677_Out_2, _Sine_741D7AFD_Out_1, _Multiply_4E6560B8_Out_2);
                                        float _Cosine_1CE8158F_Out_1;
                                        Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_1CE8158F_Out_1);
                                        float _Subtract_79877950_Out_2;
                                        Unity_Subtract_float(_Split_A3289C14_B_3, _Split_2E7606D_B_3, _Subtract_79877950_Out_2);
                                        float _Multiply_F59A2E6D_Out_2;
                                        Unity_Multiply_float(_Cosine_1CE8158F_Out_1, _Subtract_79877950_Out_2, _Multiply_F59A2E6D_Out_2);
                                        float _Add_70355923_Out_2;
                                        Unity_Add_float(_Multiply_4E6560B8_Out_2, _Multiply_F59A2E6D_Out_2, _Add_70355923_Out_2);
                                        float _Add_F983B3DB_Out_2;
                                        Unity_Add_float(_Add_70355923_Out_2, _Split_2E7606D_B_3, _Add_F983B3DB_Out_2);
                                        float3 _Vector3_829B70A3_Out_0 = float3(_Add_AFEED537_Out_2, _Split_A3289C14_G_2, _Add_F983B3DB_Out_2);
                                        OutVector3_1 = _Vector3_829B70A3_Out_0;
                                    }

                                    void Unity_Absolute_float(float In, out float Out)
                                    {
                                        Out = abs(In);
                                    }

                                    void Unity_Comparison_Less_float(float A, float B, out float Out)
                                    {
                                        Out = A < B ? 1 : 0;
                                    }

                                    void Unity_And_float(float A, float B, out float Out)
                                    {
                                        Out = A && B;
                                    }

                                    void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
                                    {
                                        Out = Predicate ? True : False;
                                    }

                                    void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                                    {
                                        Out = Predicate ? True : False;
                                    }

                                    void Unity_OneMinus_float(float In, out float Out)
                                    {
                                        Out = 1 - In;
                                    }

                                    void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
                                    {
                                        float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                                        float DITHER_THRESHOLDS[16] =
                                        {
                                            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                                            13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                                            4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                                            16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                                        };
                                        uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                                        Out = In - DITHER_THRESHOLDS[index];
                                    }

                                    // Graph Vertex
                                    // GraphVertex: <None>

                                    // Graph Pixel
                                    struct SurfaceDescriptionInputs
                                    {
                                        float3 TangentSpaceNormal;
                                        float3 WorldSpacePosition;
                                        float3 AbsoluteWorldSpacePosition;
                                        float4 ScreenPosition;
                                    };

                                    struct SurfaceDescription
                                    {
                                        float3 Albedo;
                                        float3 Emission;
                                        float Alpha;
                                        float AlphaClipThreshold;
                                    };

                                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                                    {
                                        SurfaceDescription surface = (SurfaceDescription)0;
                                        float _Property_5BD30BFD_Out_0 = _AlwaysTranslucent;
                                        float _Not_8404A126_Out_1;
                                        Unity_Not_float(_Property_5BD30BFD_Out_0, _Not_8404A126_Out_1);
                                        float3 _Property_3A9DCF63_Out_0 = _RotationAxis;
                                        float _Property_C58E76FB_Out_0 = _Rotation;
                                        Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b _NoteRotation_39534DD6;
                                        float3 _NoteRotation_39534DD6_OutVector3_1;
                                        SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(IN.AbsoluteWorldSpacePosition, _Property_3A9DCF63_Out_0, _Property_C58E76FB_Out_0, _NoteRotation_39534DD6, _NoteRotation_39534DD6_OutVector3_1);
                                        float _Split_880CB10_R_1 = _NoteRotation_39534DD6_OutVector3_1[0];
                                        float _Split_880CB10_G_2 = _NoteRotation_39534DD6_OutVector3_1[1];
                                        float _Split_880CB10_B_3 = _NoteRotation_39534DD6_OutVector3_1[2];
                                        float _Split_880CB10_A_4 = 0;
                                        float _Absolute_36A33E1A_Out_1;
                                        Unity_Absolute_float(_Split_880CB10_B_3, _Absolute_36A33E1A_Out_1);
                                        float _Property_7755A765_Out_0 = _OutlineWidth;
                                        float _Comparison_6DAA9116_Out_2;
                                        Unity_Comparison_Less_float(_Absolute_36A33E1A_Out_1, _Property_7755A765_Out_0, _Comparison_6DAA9116_Out_2);
                                        float _And_1E7C4EAD_Out_2;
                                        Unity_And_float(_Not_8404A126_Out_1, _Comparison_6DAA9116_Out_2, _And_1E7C4EAD_Out_2);
                                        float4 _Property_D57744B8_Out_0 = _OverNoteInterfaceColor;
                                        float4 _Property_41A3C666_Out_0 = _Color;
                                        float4 _Branch_BBAFE61D_Out_3;
                                        Unity_Branch_float4(_And_1E7C4EAD_Out_2, _Property_D57744B8_Out_0, _Property_41A3C666_Out_0, _Branch_BBAFE61D_Out_3);
                                        float _Property_940D9AAE_Out_0 = _AlwaysTranslucent;
                                        float _Property_3FBBCA67_Out_0 = _TranslucentAlpha;
                                        float _Property_791A452F_Out_0 = _OpaqueAlpha;
                                        float _Branch_C1EA1E3_Out_3;
                                        Unity_Branch_float(_Property_940D9AAE_Out_0, _Property_3FBBCA67_Out_0, _Property_791A452F_Out_0, _Branch_C1EA1E3_Out_3);
                                        float _OneMinus_5E47A0FD_Out_1;
                                        Unity_OneMinus_float(_Branch_C1EA1E3_Out_3, _OneMinus_5E47A0FD_Out_1);
                                        float _Dither_65B0625B_Out_2;
                                        Unity_Dither_float(_OneMinus_5E47A0FD_Out_1, float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _Dither_65B0625B_Out_2);
                                        surface.Albedo = (_Branch_BBAFE61D_Out_3.xyz);
                                        surface.Emission = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
                                        surface.Alpha = 0;
                                        surface.AlphaClipThreshold = _Dither_65B0625B_Out_2;
                                        return surface;
                                    }

                                    // --------------------------------------------------
                                    // Structs and Packing

                                    // Generated Type: Attributes
                                    struct Attributes
                                    {
                                        float3 positionOS : POSITION;
                                        float3 normalOS : NORMAL;
                                        float4 tangentOS : TANGENT;
                                        float4 uv1 : TEXCOORD1;
                                        float4 uv2 : TEXCOORD2;
                                        #if UNITY_ANY_INSTANCING_ENABLED
                                        uint instanceID : INSTANCEID_SEMANTIC;
                                        #endif
                                    };

                                    // Generated Type: Varyings
                                    struct Varyings
                                    {
                                        float4 positionCS : SV_POSITION;
                                        float3 positionWS;
                                        #if UNITY_ANY_INSTANCING_ENABLED
                                        uint instanceID : CUSTOM_INSTANCE_ID;
                                        #endif
                                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                        uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                        #endif
                                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                        uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                        #endif
                                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                        #endif
                                    };

                                    // Generated Type: PackedVaryings
                                    struct PackedVaryings
                                    {
                                        float4 positionCS : SV_POSITION;
                                        #if UNITY_ANY_INSTANCING_ENABLED
                                        uint instanceID : CUSTOM_INSTANCE_ID;
                                        #endif
                                        float3 interp00 : TEXCOORD0;
                                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                        uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                        #endif
                                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                        uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                        #endif
                                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                        FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                        #endif
                                    };

                                    // Packed Type: Varyings
                                    PackedVaryings PackVaryings(Varyings input)
                                    {
                                        PackedVaryings output = (PackedVaryings)0;
                                        output.positionCS = input.positionCS;
                                        output.interp00.xyz = input.positionWS;
                                        #if UNITY_ANY_INSTANCING_ENABLED
                                        output.instanceID = input.instanceID;
                                        #endif
                                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                        output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                        #endif
                                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                        output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                        #endif
                                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                        output.cullFace = input.cullFace;
                                        #endif
                                        return output;
                                    }

                                    // Unpacked Type: Varyings
                                    Varyings UnpackVaryings(PackedVaryings input)
                                    {
                                        Varyings output = (Varyings)0;
                                        output.positionCS = input.positionCS;
                                        output.positionWS = input.interp00.xyz;
                                        #if UNITY_ANY_INSTANCING_ENABLED
                                        output.instanceID = input.instanceID;
                                        #endif
                                        #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                        output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                        #endif
                                        #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                        output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                        #endif
                                        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                        output.cullFace = input.cullFace;
                                        #endif
                                        return output;
                                    }

                                    // --------------------------------------------------
                                    // Build Graph Inputs

                                    SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                                    {
                                        SurfaceDescriptionInputs output;
                                        ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



                                        output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


                                        output.WorldSpacePosition = input.positionWS;
                                        output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);
                                        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
                                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                                    #else
                                    #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                                    #endif
                                    #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

                                        return output;
                                    }


                                    // --------------------------------------------------
                                    // Main

                                    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
                                    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

                                    ENDHLSL
                                }

                                Pass
                                {
                                        // Name: <None>
                                        Tags
                                        {
                                            "LightMode" = "Universal2D"
                                        }

                                        // Render State
                                        Blend One Zero, One Zero
                                        Cull Back
                                        ZTest LEqual
                                        ZWrite On
                                        // ColorMask: <None>


                                        HLSLPROGRAM
                                        #pragma vertex vert
                                        #pragma fragment frag

                                        // Debug
                                        // <None>

                                        // --------------------------------------------------
                                        // Pass

                                        // Pragmas
                                        #pragma prefer_hlslcc gles
                                        #pragma exclude_renderers d3d11_9x
                                        #pragma target 2.0
                                        #pragma multi_compile_instancing

                                        // Keywords
                                        // PassKeywords: <None>
                                        // GraphKeywords: <None>

                                        // Defines
                                        #define _AlphaClip 1
                                        #define _NORMAL_DROPOFF_TS 1
                                        #define ATTRIBUTES_NEED_NORMAL
                                        #define ATTRIBUTES_NEED_TANGENT
                                        #define VARYINGS_NEED_POSITION_WS 
                                        #define SHADERPASS_2D

                                        // Includes
                                        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
                                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
                                        #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"

                                        // --------------------------------------------------
                                        // Graph

                                        // Graph Properties
                                        CBUFFER_START(UnityPerMaterial)
                                        float _AlwaysTranslucent;
                                        float3 _RotationAxis;
                                        float _Editor_IsPlaying;
                                        float _Lit;
                                        #ifdef UNITY_DOTS_INSTANCING_ENABLED
                                        float _OutlineWidth_dummy;
                                        float _TranslucentAlpha_dummy;
                                        float _OpaqueAlpha_dummy;
                                        float4 _Color_dummy;
                                        float4 _OverNoteInterfaceColor_dummy;
                                        float _Rotation_dummy;
                                        #else
                                        float _OutlineWidth;
                                        float _TranslucentAlpha;
                                        float _OpaqueAlpha;
                                        float4 _Color;
                                        float4 _OverNoteInterfaceColor;
                                        float _Rotation;
                                        #endif
                                        CBUFFER_END

                                            // Graph Functions

                                            void Unity_Not_float(float In, out float Out)
                                            {
                                                Out = !In;
                                            }

                                            void Unity_Subtract_float(float A, float B, out float Out)
                                            {
                                                Out = A - B;
                                            }

                                            void Unity_DegreesToRadians_float(float In, out float Out)
                                            {
                                                Out = radians(In);
                                            }

                                            void Unity_Cosine_float(float In, out float Out)
                                            {
                                                Out = cos(In);
                                            }

                                            void Unity_Multiply_float(float A, float B, out float Out)
                                            {
                                                Out = A * B;
                                            }

                                            void Unity_Sine_float(float In, out float Out)
                                            {
                                                Out = sin(In);
                                            }

                                            void Unity_Add_float(float A, float B, out float Out)
                                            {
                                                Out = A + B;
                                            }

                                            struct Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b
                                            {
                                            };

                                            void SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(float3 Vector3_73761834, float3 Vector3_BF93F559, float Vector1_484A216, Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b IN, out float3 OutVector3_1)
                                            {
                                                float3 _Property_345E6F87_Out_0 = Vector3_73761834;
                                                float _Split_A3289C14_R_1 = _Property_345E6F87_Out_0[0];
                                                float _Split_A3289C14_G_2 = _Property_345E6F87_Out_0[1];
                                                float _Split_A3289C14_B_3 = _Property_345E6F87_Out_0[2];
                                                float _Split_A3289C14_A_4 = 0;
                                                float3 _Property_8E8D01D1_Out_0 = Vector3_BF93F559;
                                                float _Split_2E7606D_R_1 = _Property_8E8D01D1_Out_0[0];
                                                float _Split_2E7606D_G_2 = _Property_8E8D01D1_Out_0[1];
                                                float _Split_2E7606D_B_3 = _Property_8E8D01D1_Out_0[2];
                                                float _Split_2E7606D_A_4 = 0;
                                                float _Subtract_E7BBE4F_Out_2;
                                                Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_E7BBE4F_Out_2);
                                                float _Property_513AEC1A_Out_0 = Vector1_484A216;
                                                float _DegreesToRadians_6E84C064_Out_1;
                                                Unity_DegreesToRadians_float(_Property_513AEC1A_Out_0, _DegreesToRadians_6E84C064_Out_1);
                                                float _Cosine_E2EE9560_Out_1;
                                                Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_E2EE9560_Out_1);
                                                float _Multiply_3F455942_Out_2;
                                                Unity_Multiply_float(_Subtract_E7BBE4F_Out_2, _Cosine_E2EE9560_Out_1, _Multiply_3F455942_Out_2);
                                                float _Sine_E20E60B6_Out_1;
                                                Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_E20E60B6_Out_1);
                                                float _Subtract_D0AC5623_Out_2;
                                                Unity_Subtract_float(_Split_2E7606D_B_3, _Split_A3289C14_B_3, _Subtract_D0AC5623_Out_2);
                                                float _Multiply_A4C150E8_Out_2;
                                                Unity_Multiply_float(_Sine_E20E60B6_Out_1, _Subtract_D0AC5623_Out_2, _Multiply_A4C150E8_Out_2);
                                                float _Subtract_DCFFB660_Out_2;
                                                Unity_Subtract_float(_Multiply_3F455942_Out_2, _Multiply_A4C150E8_Out_2, _Subtract_DCFFB660_Out_2);
                                                float _Add_AFEED537_Out_2;
                                                Unity_Add_float(_Subtract_DCFFB660_Out_2, _Split_2E7606D_R_1, _Add_AFEED537_Out_2);
                                                float _Subtract_EE70C677_Out_2;
                                                Unity_Subtract_float(_Split_A3289C14_R_1, _Split_2E7606D_R_1, _Subtract_EE70C677_Out_2);
                                                float _Sine_741D7AFD_Out_1;
                                                Unity_Sine_float(_DegreesToRadians_6E84C064_Out_1, _Sine_741D7AFD_Out_1);
                                                float _Multiply_4E6560B8_Out_2;
                                                Unity_Multiply_float(_Subtract_EE70C677_Out_2, _Sine_741D7AFD_Out_1, _Multiply_4E6560B8_Out_2);
                                                float _Cosine_1CE8158F_Out_1;
                                                Unity_Cosine_float(_DegreesToRadians_6E84C064_Out_1, _Cosine_1CE8158F_Out_1);
                                                float _Subtract_79877950_Out_2;
                                                Unity_Subtract_float(_Split_A3289C14_B_3, _Split_2E7606D_B_3, _Subtract_79877950_Out_2);
                                                float _Multiply_F59A2E6D_Out_2;
                                                Unity_Multiply_float(_Cosine_1CE8158F_Out_1, _Subtract_79877950_Out_2, _Multiply_F59A2E6D_Out_2);
                                                float _Add_70355923_Out_2;
                                                Unity_Add_float(_Multiply_4E6560B8_Out_2, _Multiply_F59A2E6D_Out_2, _Add_70355923_Out_2);
                                                float _Add_F983B3DB_Out_2;
                                                Unity_Add_float(_Add_70355923_Out_2, _Split_2E7606D_B_3, _Add_F983B3DB_Out_2);
                                                float3 _Vector3_829B70A3_Out_0 = float3(_Add_AFEED537_Out_2, _Split_A3289C14_G_2, _Add_F983B3DB_Out_2);
                                                OutVector3_1 = _Vector3_829B70A3_Out_0;
                                            }

                                            void Unity_Absolute_float(float In, out float Out)
                                            {
                                                Out = abs(In);
                                            }

                                            void Unity_Comparison_Less_float(float A, float B, out float Out)
                                            {
                                                Out = A < B ? 1 : 0;
                                            }

                                            void Unity_And_float(float A, float B, out float Out)
                                            {
                                                Out = A && B;
                                            }

                                            void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
                                            {
                                                Out = Predicate ? True : False;
                                            }

                                            void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                                            {
                                                Out = Predicate ? True : False;
                                            }

                                            void Unity_OneMinus_float(float In, out float Out)
                                            {
                                                Out = 1 - In;
                                            }

                                            void Unity_Dither_float(float In, float4 ScreenPosition, out float Out)
                                            {
                                                float2 uv = ScreenPosition.xy * _ScreenParams.xy;
                                                float DITHER_THRESHOLDS[16] =
                                                {
                                                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                                                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                                                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                                                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                                                };
                                                uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                                                Out = In - DITHER_THRESHOLDS[index];
                                            }

                                            // Graph Vertex
                                            // GraphVertex: <None>

                                            // Graph Pixel
                                            struct SurfaceDescriptionInputs
                                            {
                                                float3 TangentSpaceNormal;
                                                float3 WorldSpacePosition;
                                                float3 AbsoluteWorldSpacePosition;
                                                float4 ScreenPosition;
                                            };

                                            struct SurfaceDescription
                                            {
                                                float3 Albedo;
                                                float Alpha;
                                                float AlphaClipThreshold;
                                            };

                                            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                                            {
                                                SurfaceDescription surface = (SurfaceDescription)0;
                                                float _Property_5BD30BFD_Out_0 = _AlwaysTranslucent;
                                                float _Not_8404A126_Out_1;
                                                Unity_Not_float(_Property_5BD30BFD_Out_0, _Not_8404A126_Out_1);
                                                float3 _Property_3A9DCF63_Out_0 = _RotationAxis;
                                                float _Property_C58E76FB_Out_0 = _Rotation;
                                                Bindings_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b _NoteRotation_39534DD6;
                                                float3 _NoteRotation_39534DD6_OutVector3_1;
                                                SG_NoteRotation_8aaa231d52fa79042b5a9de9f8bd840b(IN.AbsoluteWorldSpacePosition, _Property_3A9DCF63_Out_0, _Property_C58E76FB_Out_0, _NoteRotation_39534DD6, _NoteRotation_39534DD6_OutVector3_1);
                                                float _Split_880CB10_R_1 = _NoteRotation_39534DD6_OutVector3_1[0];
                                                float _Split_880CB10_G_2 = _NoteRotation_39534DD6_OutVector3_1[1];
                                                float _Split_880CB10_B_3 = _NoteRotation_39534DD6_OutVector3_1[2];
                                                float _Split_880CB10_A_4 = 0;
                                                float _Absolute_36A33E1A_Out_1;
                                                Unity_Absolute_float(_Split_880CB10_B_3, _Absolute_36A33E1A_Out_1);
                                                float _Property_7755A765_Out_0 = _OutlineWidth;
                                                float _Comparison_6DAA9116_Out_2;
                                                Unity_Comparison_Less_float(_Absolute_36A33E1A_Out_1, _Property_7755A765_Out_0, _Comparison_6DAA9116_Out_2);
                                                float _And_1E7C4EAD_Out_2;
                                                Unity_And_float(_Not_8404A126_Out_1, _Comparison_6DAA9116_Out_2, _And_1E7C4EAD_Out_2);
                                                float4 _Property_D57744B8_Out_0 = _OverNoteInterfaceColor;
                                                float4 _Property_41A3C666_Out_0 = _Color;
                                                float4 _Branch_BBAFE61D_Out_3;
                                                Unity_Branch_float4(_And_1E7C4EAD_Out_2, _Property_D57744B8_Out_0, _Property_41A3C666_Out_0, _Branch_BBAFE61D_Out_3);
                                                float _Property_940D9AAE_Out_0 = _AlwaysTranslucent;
                                                float _Property_3FBBCA67_Out_0 = _TranslucentAlpha;
                                                float _Property_791A452F_Out_0 = _OpaqueAlpha;
                                                float _Branch_C1EA1E3_Out_3;
                                                Unity_Branch_float(_Property_940D9AAE_Out_0, _Property_3FBBCA67_Out_0, _Property_791A452F_Out_0, _Branch_C1EA1E3_Out_3);
                                                float _OneMinus_5E47A0FD_Out_1;
                                                Unity_OneMinus_float(_Branch_C1EA1E3_Out_3, _OneMinus_5E47A0FD_Out_1);
                                                float _Dither_65B0625B_Out_2;
                                                Unity_Dither_float(_OneMinus_5E47A0FD_Out_1, float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _Dither_65B0625B_Out_2);
                                                surface.Albedo = (_Branch_BBAFE61D_Out_3.xyz);
                                                surface.Alpha = 0;
                                                surface.AlphaClipThreshold = _Dither_65B0625B_Out_2;
                                                return surface;
                                            }

                                            // --------------------------------------------------
                                            // Structs and Packing

                                            // Generated Type: Attributes
                                            struct Attributes
                                            {
                                                float3 positionOS : POSITION;
                                                float3 normalOS : NORMAL;
                                                float4 tangentOS : TANGENT;
                                                #if UNITY_ANY_INSTANCING_ENABLED
                                                uint instanceID : INSTANCEID_SEMANTIC;
                                                #endif
                                            };

                                            // Generated Type: Varyings
                                            struct Varyings
                                            {
                                                float4 positionCS : SV_POSITION;
                                                float3 positionWS;
                                                #if UNITY_ANY_INSTANCING_ENABLED
                                                uint instanceID : CUSTOM_INSTANCE_ID;
                                                #endif
                                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                                #endif
                                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                                #endif
                                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                                #endif
                                            };

                                            // Generated Type: PackedVaryings
                                            struct PackedVaryings
                                            {
                                                float4 positionCS : SV_POSITION;
                                                #if UNITY_ANY_INSTANCING_ENABLED
                                                uint instanceID : CUSTOM_INSTANCE_ID;
                                                #endif
                                                float3 interp00 : TEXCOORD0;
                                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                                                #endif
                                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                                                #endif
                                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                                                #endif
                                            };

                                            // Packed Type: Varyings
                                            PackedVaryings PackVaryings(Varyings input)
                                            {
                                                PackedVaryings output = (PackedVaryings)0;
                                                output.positionCS = input.positionCS;
                                                output.interp00.xyz = input.positionWS;
                                                #if UNITY_ANY_INSTANCING_ENABLED
                                                output.instanceID = input.instanceID;
                                                #endif
                                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                                #endif
                                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                                #endif
                                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                                output.cullFace = input.cullFace;
                                                #endif
                                                return output;
                                            }

                                            // Unpacked Type: Varyings
                                            Varyings UnpackVaryings(PackedVaryings input)
                                            {
                                                Varyings output = (Varyings)0;
                                                output.positionCS = input.positionCS;
                                                output.positionWS = input.interp00.xyz;
                                                #if UNITY_ANY_INSTANCING_ENABLED
                                                output.instanceID = input.instanceID;
                                                #endif
                                                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                                                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                                                #endif
                                                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                                                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                                                #endif
                                                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                                output.cullFace = input.cullFace;
                                                #endif
                                                return output;
                                            }

                                            // --------------------------------------------------
                                            // Build Graph Inputs

                                            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                                            {
                                                SurfaceDescriptionInputs output;
                                                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



                                                output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


                                                output.WorldSpacePosition = input.positionWS;
                                                output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);
                                                output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
                                            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                                            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                                            #else
                                            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                                            #endif
                                            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

                                                return output;
                                            }


                                            // --------------------------------------------------
                                            // Main

                                            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
                                            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

                                            ENDHLSL
                                        }

    }
        CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
                                                FallBack "Hidden/Shader Graph/FallbackError"
}
