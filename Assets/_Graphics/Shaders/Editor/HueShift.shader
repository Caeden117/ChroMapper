// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

Shader "BSUI/ColorSquare" {
	Properties{
		_MainTex("Main Texture", 2D) = "white" {}
		_Hue("Hue", Range(0,1)) = 0
	}
		SubShader{
			Lighting Off
			Blend One SrcColor

			Pass{
				CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0

		#include "UnityCG.cginc"

				//Prepare the inputs
				struct vertIN {
				float4 vertex : POSITION;
				float4 texcoord0 : TEXCOORD0;
			};

			struct fragIN {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			//Get the values from outside
			float _Hue;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			//Fill the vert struct
			fragIN vert(vertIN v) {
				fragIN o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord0;

				return o;
			}

			fixed4 frag(fragIN i) : COLOR{

				fixed3 c = tex2D(_MainTex, i.uv);

				fixed PI = 3.14159265359; 
				float angle = _Hue * 2 * PI;

				float3 k = float3(0.57735, 0.57735, 0.57735);
				float cosAngle = cos(angle);

				c = c * cosAngle + cross(k, c) * sin(angle) + k * dot(k, c) * (1 - cosAngle);
				return float4(c,0);
			}

			ENDCG

		}
	}
}