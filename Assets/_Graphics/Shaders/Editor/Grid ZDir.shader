// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Grid ZDir" {

	Properties{
		_GridThickness("Grid Thickness", Float) = 0.01
		_GridSpacing("Grid Spacing", Float) = 10.0
		_GridColour("Grid Colour", Color) = (0.5, 1.0, 1.0, 1.0)
		_BaseColour("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
		_BeatsPerBar("Beats Per Bar", Float) = 16
		_Offset("Offset", Float) = 0
	}

	SubShader{
		Tags{ "Queue" = "Transparent" }

		Pass{
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		// Define the vertex and fragment shader functions
		#pragma vertex vert
		#pragma fragment frag

		// Access Shaderlab properties
		uniform float _GridThickness;
		uniform float _GridSpacing;
		uniform float _BeatsPerBar;
		uniform float _Offset;
		uniform float4 _GridColour;
		uniform float4 _BaseColour;

		// Input into the vertex shader
		struct vertexInput {
			float4 vertex : POSITION;
		};

		// Output from vertex shader into fragment shader
		struct vertexOutput {
			float4 pos : SV_POSITION;
			float4 worldPos : TEXCOORD0;
		};

		// VERTEX SHADER
		vertexOutput vert(vertexInput input) {
			vertexOutput output;
			output.pos = UnityObjectToClipPos(input.vertex);
			// Calculate the world position coordinates to pass to the fragment shader
			output.worldPos = mul(unity_ObjectToWorld, input.vertex);
			return output;
		}

		// FUCKING WIZARD CODE //
		float4 frag(vertexOutput input) : COLOR{
			float offset = frac(_Offset);
			if (frac((input.worldPos.z / _GridSpacing) + offset) <= _GridThickness / 2 || frac((input.worldPos.z / _GridSpacing) + offset) >= 1 - (_GridThickness / 2)) {
				return _GridColour;
			} else {
				return _BaseColour;
			}
		}
			ENDCG
		}
	}

		//Old coordinate code
		// FRAGMENT SHADER
		/*float4 frag(vertexOutput input) : COLOR{
			float offset = frac(_Offset);
			if (frac((input.worldPos.z / _GridSpacing) + offset) <= _GridThickness / 2) {
				if (offset == 0) return _GridColour;
				if (1 - frac((input.worldPos.z / _GridSpacing) + offset) <= 1 - ((_GridThickness / 2) - offset))
					return _GridColour;
				else
					return _BaseColour;
			} else {
				return _BaseColour;
			}
		}
			ENDCG
		}*/

}