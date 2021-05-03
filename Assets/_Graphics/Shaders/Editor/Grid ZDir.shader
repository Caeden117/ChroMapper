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
		_EditorScale("Editor Scale", Float) = 4
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
			uniform float _BPMChange_Times[1963];
			uniform float _BPMChange_BPMs[1963];
			uniform int _BPMChange_Count;
			uniform float _EditorScale;

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
				//Get rotation in radians (this is used for 360/90 degree map rotation).
				float rotationInRadians = _Rotation * (3.141592653 / 180);
				//Transform X and Z around global platform offset (2D rotation PogU)
				float newX = (output.worldPos.x - offset.x) * cos(rotationInRadians) - (output.worldPos.z - offset.z) * sin(rotationInRadians);
				float newZ = (output.worldPos.z - offset.z) * cos(rotationInRadians) + (output.worldPos.x - offset.x) * sin(rotationInRadians);
				output.rotatedPos = float4(newX + offset.x, output.worldPos.y, newZ + offset.z, output.worldPos.w);
				return output;
			}

			// FUCKING WIZARD CODE //
			float4 frag(vertexOutput input) : COLOR{
				float editorScaleMult = (_EditorScale / 4);
				//WHERE'S THE LAMB SAUCE (unedited beat time)
				float timeButRAWWW = (input.rotatedPos.z + _Offset) / _EditorScale;
				//To plugerino into shader after dealing with BPM Changes
				float time = timeButRAWWW;
				if (_BPMChange_BPMs[1] > 0)
				{
					time = 0;
					for (int i = 0; i < _BPMChange_Count - 1; i++)
					{
						float currBpmTime = _BPMChange_Times[i];
						float nextBpmTime = _BPMChange_Times[i + 1];
						if (timeButRAWWW < 0) //Check for negative beats
						{
							time = timeButRAWWW;
							break;
						}
						else if (currBpmTime < timeButRAWWW && nextBpmTime > timeButRAWWW)
						{
							float difference = timeButRAWWW - currBpmTime;
							float timeInSecond = (60 / _BPMChange_BPMs[0]) * difference;
							float timeInNewBeat = (_BPMChange_BPMs[i] / 60) * timeInSecond;
							time = timeInNewBeat;
						}
					}
					float lastBpmTime = _BPMChange_Times[_BPMChange_Count - 1];
					if (lastBpmTime < timeButRAWWW)
					{
						float difference = timeButRAWWW - lastBpmTime;
						float timeInSecond = (60 / _BPMChange_BPMs[0]) * difference;
						float timeInNewBeat = (_BPMChange_BPMs[_BPMChange_Count - 1] / 60) * timeInSecond;
						time = timeInNewBeat;
					}
				}
				if ((abs(time * editorScaleMult) % _GridSpacing) / _GridSpacing <= _GridThickness / 2 || (abs(time * editorScaleMult) % _GridSpacing) / _GridSpacing >= 1 - (_GridThickness / 2)) {
					return _GridColour;
				} else {
					return _BaseColour;
				}
			}
			ENDCG
		}
	}
}
