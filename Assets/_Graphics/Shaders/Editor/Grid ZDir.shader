// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Grid ZDir" {

	Properties{
		_GridThickness("Grid Thickness", Float) = 0.01
		_GridSpacing("Grid Spacing", Float) = 10.0
		_GridColour("Grid Colour", Color) = (1.0, 1.0, 1.0, 0.4)
		_BaseColour("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
		_Offset("Offset", Float) = 0
		_Rotation("Rotation", Float) = 0
	}

	SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent"}

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off

			CGPROGRAM

			// Define the vertex and fragment shader functions
			#pragma vertex vert
			#pragma fragment frag

			// Access Shaderlab properties
			uniform float _GridThickness;
			uniform float _GridSpacing;
			uniform float _Offset;
			uniform float4 _GridColour;
			uniform float4 _BaseColour;
			uniform float _Rotation;
			uniform float _BPMChange_Times[2046];
			uniform float _BPMChange_BPMs[2046];
			uniform int _BPMChange_Count;

			// Input into the vertex shader
			struct vertexInput {
				float4 vertex : POSITION;
			};

			// Output from vertex shader into fragment shader
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
				float4 rotatedPos : TEXCOORD1;
			};

			// VERTEX SHADER
			vertexOutput vert(vertexInput input) {
				vertexOutput output;
				output.pos = UnityObjectToClipPos(input.vertex);
				// Calculate the world position coordinates to pass to the fragment shader
				output.worldPos = mul(unity_ObjectToWorld, input.vertex);
				
				//Global platform offset
				float4 offset = float4(0, -0.5, -1.5, 0);
				//Get rotation in radians.
				float rotationInRadians = _Rotation * (3.141592653 / 180);
				//Transform X and Z around global platform offset (2D rotation PogU)
				float newX = (output.worldPos.x - offset.x) * cos(rotationInRadians) - (output.worldPos.z - offset.z) * sin(rotationInRadians);
				float newZ = (output.worldPos.z - offset.z) * cos(rotationInRadians) + (output.worldPos.x - offset.x) * sin(rotationInRadians);
				output.rotatedPos = float4(newX + offset.x, output.worldPos.y, newZ + offset.z, output.worldPos.w);
				return output;
			}

			// FUCKING WIZARD CODE //
			float4 frag(vertexOutput input) : COLOR{
				float timeButRAWWW = input.rotatedPos.z + _Offset;
				float time = timeButRAWWW;
				if (_BPMChange_BPMs[1] > 0)
				{
					for (int i = 0; i < _BPMChange_Count - 1; i++)
					{
						if (timeButRAWWW < 0)
						{
							time = timeButRAWWW;
							break;
						}
						else if (_BPMChange_Times[i] < timeButRAWWW && _BPMChange_Times[i + 1] > timeButRAWWW)
						{
							float difference = timeButRAWWW - _BPMChange_Times[i];
							float timeInSecond = (60 / _BPMChange_BPMs[0]) * difference;
							float timeInNewBeat = (_BPMChange_BPMs[i] / 60) * timeInSecond;
							time = time + timeInNewBeat;
						}
						else if (_BPMChange_Times[i] < timeButRAWWW && _BPMChange_Times[i + 1] < timeButRAWWW)
						{
							float difference = _BPMChange_Times[i + 1] - _BPMChange_Times[i];
							float timeInSecond = (60 / _BPMChange_BPMs[0]) * difference;
							float timeInNewBeat = (_BPMChange_BPMs[i] / 60) * timeInSecond;
							time = time + timeInNewBeat;
						}
					}
					if (_BPMChange_Times[_BPMChange_Count - 1] < timeButRAWWW)
					{
						float difference = timeButRAWWW - _BPMChange_Times[_BPMChange_Count - 1];
						float timeInSecond = (60 / _BPMChange_BPMs[0]) * difference;
						float timeInNewBeat = (_BPMChange_BPMs[_BPMChange_Count - 1] / 60) * timeInSecond;
						time = time + timeInNewBeat;
					}
				}
				if (time <= 0)
				{
					if (frac(time / _GridSpacing) <= _GridThickness / 2 || frac(time / _GridSpacing) >= 1 - (_GridThickness / 2)) {
						return _GridColour;
					} else {
						return _BaseColour;
					}
				}
				if ((time % _GridSpacing) / _GridSpacing <= _GridThickness / 2 || (time % _GridSpacing) / _GridSpacing >= 1 - (_GridThickness / 2)) {
					return _GridColour;
				} else {
					return _BaseColour;
				}
			}
			ENDCG
		}
	}
}