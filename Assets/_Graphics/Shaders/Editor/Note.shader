// Example Shader for Universal RP
// Written by @Cyanilux
// https://cyangamedev.wordpress.com/urp-shader-code/
Shader "Custom/Note"
{
	Properties
	{
		_OutlineWidth("OutlineWidth", Float) = 0.05
		_TranslucentAlpha("TranslucentAlpha", Float) = 0.5
		_OpaqueAlpha("OpaqueAlpha", Float) = 1
		_Color("NoteColor", Color) = (0, 0, 0, 0)
		_OverNoteInterfaceColor("OverNoteInterfaceColor", Color) = (1, 1, 1, 0)
		_Rotation("Rotation", Float) = 0
		[Toggle] _Lit("Lit", Float) = 0
		[Toggle] _AlwaysTranslucent("AlwaysTranslucent", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
			#pragma multi_compile_instancing

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
				UNITY_DEFINE_INSTANCED_PROP(float4, _OverNoteInterfaceColor)
				UNITY_DEFINE_INSTANCED_PROP(float, _OutlineWidth)
				UNITY_DEFINE_INSTANCED_PROP(float, _TranslucentAlpha)
				UNITY_DEFINE_INSTANCED_PROP(float, _OpaqueAlpha)
				UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
				UNITY_DEFINE_INSTANCED_PROP(float, _Lit)
				UNITY_DEFINE_INSTANCED_PROP(float, _AlwaysTranslucent)
			UNITY_INSTANCING_BUFFER_END(Props)
		ENDHLSL

		Pass
		{
			Name "Example"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM

			// Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles

			//#pragma target 4.5 // https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html

			#pragma vertex vert
			#pragma fragment frag

			// Material Keywords
			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _RECEIVE_SHADOWS_OFF

			// URP Keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float3 normalOS		: NORMAL;
				float4 tangentOS	: TANGENT;
			};

			struct Varyings
			{
				float4 positionCS				: SV_POSITION;
				float4 ScreenPosition           : POSITION1;
				float4 rotatedPos               : POSITION2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				float3 positionWS			    : TEXCOORD2;
				float3 normalWS					: TEXCOORD3;
				float3 viewDirWS 				: TEXCOORD4;
			};

			// Automatically defined with SurfaceInput.hlsl
			//TEXTURE2D(_BaseMap);
			//SAMPLER(sampler_BaseMap);

			#if SHADER_LIBRARY_VERSION_MAJOR < 9
			// This function was added in URP v9.x.x versions, if we want to support URP versions before, we need to handle it instead.
			// Computes the world space view direction (pointing towards the viewer).
			float3 GetWorldSpaceViewDir(float3 positionWS)
			{
				if (unity_OrthoParams.w == 0)
				{
					// Perspective
					return _WorldSpaceCameraPos - positionWS;
				}
				else
				{
					// Orthographic
					float4x4 viewMat = GetWorldToViewMatrix();
					return viewMat[2].xyz;
				}
			}
			#endif

			Varyings vert(Attributes IN)
			{
				Varyings OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT); // necessary only if you want to access instanced properties in the fragment Shader.

				VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = positionInputs.positionCS;

				float4 ScreenPosition = ComputeScreenPos(OUT.positionCS);
				OUT.ScreenPosition = ScreenPosition;

				OUT.positionWS = positionInputs.positionWS;
					
                //Global platform offset
                float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = (OUT.positionWS.x - offset.x) * cos(rotationInRadians) - (OUT.positionWS.z - offset.z) * sin(rotationInRadians);
                float newZ = (OUT.positionWS.z - offset.z) * cos(rotationInRadians) + (OUT.positionWS.x - offset.x) * sin(rotationInRadians);

				OUT.rotatedPos = float4(newX + offset.x, OUT.positionWS.y, newZ + offset.z, 0);

				OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

				VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
				OUT.normalWS = normalInputs.normalWS;

				OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

				return OUT;
			}

			InputData InitializeInputData(Varyings IN, half3 normalTS)
			{
				UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
				
				InputData inputData = (InputData)0;

				inputData.positionWS = IN.positionWS;

				half3 viewDirWS = SafeNormalize(IN.viewDirWS);
				inputData.normalWS = IN.normalWS;

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = viewDirWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					inputData.shadowCoord = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
				return inputData;
			}

			SurfaceData InitializeSurfaceData(Varyings IN)
			{
				UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

				SurfaceData surfaceData = (SurfaceData)0;
				// Note, we can just use SurfaceData surfaceData; here and not set it.
				// However we then need to ensure all values in the struct are set before returning.
				// By casting 0 to SurfaceData, we automatically set all the contents to 0.
				// DON'T BE CONFUSED THE COLOR IS ACTUALLY SET HERE
				surfaceData.alpha = 1;

				float isTranslucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);
				float4 interfaceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverNoteInterfaceColor);
				float4 noteColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				float outlineWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineWidth);
				float rotatedZ = abs(IN.rotatedPos.z);

				surfaceData.albedo = (rotatedZ < outlineWidth && isTranslucent < 1) ? interfaceColor : noteColor.rgb;

				// For the sake of simplicity I'm not supporting the metallic/specular map or occlusion map
				// for an example of that see : https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl

				float lit = UNITY_ACCESS_INSTANCED_PROP(Props, _Lit);

				surfaceData.metallic = lit == 1 ? 0.5 : 0;
				surfaceData.smoothness = lit == 1 ? 0.7 : 0;
				surfaceData.occlusion = 1;

				return surfaceData;
			}

			float isDithered(float2 pos, float alpha) {
				pos *= _ScreenParams.xy;

				// Define a dither threshold matrix which can
				// be used to define how a 4x4 set of pixels
				// will be dithered
				float DITHER_THRESHOLDS[16] =
				{
					1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
					13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
					4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
					16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
				};

				int index = (int(pos.x) % 4) * 4 + int(pos.y) % 4;
				return alpha - DITHER_THRESHOLDS[index];
			}

			half4 frag(Varyings IN) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

				SurfaceData surfaceData = InitializeSurfaceData(IN);
				InputData inputData = InitializeInputData(IN, surfaceData.normalTS);

				// In URP v10+ versions we could use this :
				// half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// But for other versions, we need to use this instead.
				// We could also avoid using the SurfaceData struct completely, but it helps to organise things.
				half4 color = UniversalFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic,
					surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion,
					surfaceData.emission, surfaceData.alpha);

				float isTranslucent = UNITY_ACCESS_INSTANCED_PROP(Props, _AlwaysTranslucent);

				float translucentAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _TranslucentAlpha);
				float opaqueAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _OpaqueAlpha);
				
				float alpha = isTranslucent >= 1 ? translucentAlpha : opaqueAlpha;

				clip(isDithered(IN.ScreenPosition.xy / IN.ScreenPosition.w, alpha));

				return color; // float4(inputData.bakedGI,1);
			}

			ENDHLSL
		}
	}
}
