Shader "Unlit/Reveal"
{
	Properties
	{
		_MainTex ("SelfIllum Color (RGB) Alpha (A)", 2D) = "white" {}
		_ColorTint ("Color Tint", Color) = (1, 0, 0, 0)
		_Position("Point Position", Vector) = (0, 0, 0, 0)
		_CircleRadius("Spotlight Size", Range(0, 20)) = 0.2
		_FadeSize("Fade Size", Range(0, 5)) = 0.5
		_MainAlpha("Main Alpha", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float dist : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			float4 _Position;
			float _CircleRadius;
			float _FadeSize;
			float _MainAlpha;
			float4 _ColorTint;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				//o.dist = distance(worldPos, _Position.xyz);
				o.dist = abs(_Position.z - worldPos.z);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = (1, 0, 0, 0);
				col = _ColorTint;

				//Spotlight rendering
				if (i.dist < _CircleRadius)	
					col.a = _MainAlpha;
				//Blending between spotlight and outside
				else if (i.dist > _CircleRadius && i.dist < _CircleRadius + _FadeSize)
				{
					float blendStrength = i.dist - _CircleRadius;
					col.a = lerp(_MainAlpha, 0, blendStrength / _FadeSize);
				}
				else {
					col.a = 0;
				}

				return col;
			}
			ENDCG
		}
	}
}
