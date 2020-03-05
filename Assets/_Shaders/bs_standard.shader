// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "BeatSaber/Standard"
{
	Properties
    {
		// Glow
		_GlowTex ("Glow Map", 2D) = "white" {}
		
		// Albedo
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo", 2D) = "white" {}
		[NoScaleOffset] _NormalTex ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Bump Scale", Float) = 1

		// Rim Lighting Parameters
		_Scale ("Rimlight Scale", Range(0, 5)) = 4.0
		_Power ("Rimlight Power", Range(0, 10)) = 4.0

		// Lighting Parameters
		_Ambient ("Ambient", Range(0, 1)) = 0.0
		_LightIntensity ("Light Intensity", Float) = 1
		_LightDir ("Light Direction", Vector) = (0,-1,-2,0)

		// Surface Parameters
		[NoScaleOffset]_MetalTex ("Metallic", 2D) = "white" {}
		[Gamma]_Metallic ("Metallic", Range(0,1)) = 0
		_Smoothness ("Smoothness", Range(0,1)) = 0.5

    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
       
        GrabPass { }
       
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

			#pragma shader_feature _METALLIC_MAP
			#pragma shader_feature _GLOW
 
			#include "UnityPBSLighting.cginc"

			// Color and Glow
			fixed4 _Color;
			sampler2D _GlowTex;
			
			// Albedo
			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			// Normals
			sampler2D _NormalTex;
			float _BumpScale;

			// Normals
			sampler2D _MetalTex;

			// Surface Properties
			float _Metallic;
			float _Smoothness;

			// Lighting
			fixed _Ambient;
			fixed _LightIntensity;
			fixed4 _LightDir;

			// RimLight
			fixed _Scale;
			fixed _Power;

			// GrabPass Texture for Light approximation
			sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
				float4 uvscreen : TEXCOORD1;
				float4 tangent : TEXCOORD2;
				float3 normal : NORMAL;
                float3 viewDir : TEXCOORD3;
				float3 VSNormal : TEXCOORD4;
            };
 
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvscreen = ComputeGrabScreenPos(o.pos);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				o.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
				o.VSNormal = COMPUTE_VIEW_NORMAL;
				o.viewDir = normalize(WorldSpaceViewDir (v.vertex));
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
				
				// Normals
				float3 tangentSpaceNormal = UnpackScaleNormal(tex2D(_NormalTex, i.uv), _BumpScale);

				float binormal = cross(i.normal, i.tangent.xyz) * i.tangent.w;

				i.normal = normalize(
				tangentSpaceNormal.x * i.tangent +
				tangentSpaceNormal.y * binormal +
				tangentSpaceNormal.z * i.normal
				);

				// screen normals
				binormal = cross(i.VSNormal, i.tangent.xyz) * i.tangent.w;

				i.VSNormal = normalize(
				tangentSpaceNormal.x * i.tangent +
				tangentSpaceNormal.y * binormal +
				tangentSpaceNormal.z * i.VSNormal
				);

				i.viewDir = normalize(i.viewDir);

				float4 pixelCol = half4(0, 0, 0, 0);
				
				float _Factor = 50;
				// Shorthand for mixing pixels by weight and blur
				#define ADDPIXELXY(weight,kernelX,kernelY) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD( float4( i.uvscreen.x + (i.VSNormal.x + kernelX * (1-_Smoothness)) * _Factor * _GrabTexture_TexelSize.x,  i.uvscreen.y + (-i.VSNormal.y + kernelY * (1-_Smoothness)) * _Factor * _GrabTexture_TexelSize.y, i.uvscreen.z, i.uvscreen.w))) * weight
				// average 9 points for local reflections
				pixelCol += ADDPIXELXY(0.05, 1.0, -1.0);
				pixelCol += ADDPIXELXY(0.10, 1.0, 0.0);
				pixelCol += ADDPIXELXY(0.11, 1.0, 1.0);
				pixelCol += ADDPIXELXY(0.15, 0.0, -1.0);
				pixelCol += ADDPIXELXY(0.18, 0.0, 0.0);
				pixelCol += ADDPIXELXY(0.15, 0.0, 1.0);
				pixelCol += ADDPIXELXY(0.11, -1.0, -1.0);
				pixelCol += ADDPIXELXY(0.10, -1.0, 0.0);
				pixelCol += ADDPIXELXY(0.05, -1.0, 1.0);

				// get average colour in background for approximating lighting colour
				int sampleRange = 3; // square root of total samples, -don't go crazy-
				float4 lightColor = half4(0,0,0,0);

				// Shorthand for mixing pixels by weight by screen position
				#define ADDPIXELXYSCREEN(weight,kernelX,kernelY) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(_GrabTexture_TexelSize.z / (sampleRange + 1.0) * (kernelX + 1.0), _GrabTexture_TexelSize.w / (sampleRange + 1.0) * (kernelY + 1.0), _GrabTexture_TexelSize.z, _GrabTexture_TexelSize.w))) * weight
                
				// naughty for loop to get samples from the grab pass
				for(int k = 0; k < sampleRange; k++){
					for(int j = 0; j < sampleRange; j++){
						lightColor += ADDPIXELXYSCREEN(1/sampleRange/sampleRange, k, j);
					}
				}
				
				lightColor *= _LightIntensity;
				
				// fresnel mask
				float fresnel = _Scale * pow(1.0 - dot(i.viewDir, i.normal), _Power);

				// directional Light
				float3 lightDir = normalize(_LightDir.xyz) * -1.0;

				// albedo Texture
				float3 tex = tex2D(_MainTex, i.uv) * _Color;

				// Metallic
				#if defined(_METALLIC_MAP)
					float metallic = tex2D(_MetalTex, i.uv.xy).r;
					float smoothness = tex2D(_MetalTex, i.uv.xy).a;
				#else
					float metallic = _Metallic;
					float smoothness = _Smoothness;
				#endif // _METALLIC_MAP

				// Specular
				float3 specularTint;
				float oneMinusReflectivity;
				float albedo = DiffuseAndSpecularFromMetallic(
					tex, metallic, specularTint, oneMinusReflectivity
				);
				
				UnityLight light;
				light.color = lightColor;
				light.dir = lightDir;
				light.ndotl = DotClamped(i.normal, lightDir);

				UnityIndirect indirectLight;
				indirectLight.diffuse = _Ambient * lightColor;
				indirectLight.specular = _Ambient * lightColor;

				float3 pb = UNITY_BRDF_PBS(
					albedo, specularTint,
					oneMinusReflectivity, smoothness,
					i.normal, i.viewDir,
					light, indirectLight
				);

				pb += fresnel * pixelCol;

				#if defined(_GLOW)
					float alpha = tex2D(_GlowTex, i.uv).g;
					return float4(lerp(pb, tex, alpha), alpha);
				#else
					return float4(pb, 0.0);
				#endif // _GLOW
            }
            ENDCG
        }
    }
	CustomEditor "BsStandardEditor"
}