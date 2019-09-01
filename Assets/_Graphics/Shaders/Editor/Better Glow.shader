// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//This version of the shader does not support shadows, but it does support transparent outlines

Shader "Outlined/UltimateOutline"
{
	Properties
	{
		_Color("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex("Texture", 2D) = "white" {}

		_FirstOutlineColor("Outline color", Color) = (1,0,0,0.5)
		_FirstOutlineWidth("Outlines width", Range(0.0, 2.0)) = 0.15

		_Angle("Switch shader on angle", Range(0.0, 180.0)) = 89
	}

	CGINCLUDE
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct appdata members uv_MainTex,viewDir)
#pragma exclude_renderers d3d11
		#include "UnityCG.cginc"

		struct appdata {
			float4 vertex : POSITION;
			float4 normal : NORMAL;
			float2 uv_MainTex;
			float3 viewDir;
		};

		uniform float4 _FirstOutlineColor;
		uniform float _FirstOutlineWidth;

		uniform sampler2D _MainTex;
		uniform float4 _Color;
		uniform float _Angle;

	ENDCG

	SubShader{
		//Surface shader

		Pass{
			Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				//UNITY_FOG_COORDS(1)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//fixed4 col = _Color;
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return _Color;
			}
			ENDCG
		}

		//First outline
		Pass{
			Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			Cull Back
			CGPROGRAM

			struct v2f {
				float4 pos : SV_POSITION;
				float dist : TEXCOORD0;
			};

			#pragma vertex vert
			#pragma fragment frag

			v2f vert(appdata v) {
				appdata original = v;

				float3 scaleDir = normalize(v.vertex.xyz - float4(0,0,0,1));
				//scaleDir = float3(scaleDir.x, 0.0175, scaleDir.z);
				float3 originalPos = UnityObjectToClipPos(v.vertex).xyz;
				//This shader consists of 2 ways of generating outline that are dynamically switched based on demiliter angle
				//If vertex normal is pointed away from object origin then custom outline generation is used (based on scaling along the origin-vertex vector)
				//Otherwise the old-school normal vector scaling is used
				//This way prevents weird artifacts from being created when using either of the methods
				if (degrees(acos(dot(scaleDir.xyz, v.normal.xyz))) > _Angle) {
					v.vertex.xyz += normalize(v.normal.xyz) * _FirstOutlineWidth;
				}
				else {
					v.vertex.xyz += scaleDir * _FirstOutlineWidth;
				}
				v.vertex.xyz = float3(v.vertex.xyz.x, v.vertex.xyz.y - (scaleDir.y * _FirstOutlineWidth), v.vertex.xyz.z);
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.dist = distance(originalPos, o.pos);

				o.Normal = UnpackNormal(tex2D(_MainTex, v.uv_MainTex));
				half rim = saturate(dot(scaleDir.xyz, v.normal.xyz));
				o.Emission = _FirstOutlineColor.rgb * pow(rim, _RimPower);
				return o;
			}

			half4 frag(v2f i) : COLOR{
				return _FirstOutlineColor;
			}

			ENDCG
		}

		
	}
	Fallback "Diffuse"
}