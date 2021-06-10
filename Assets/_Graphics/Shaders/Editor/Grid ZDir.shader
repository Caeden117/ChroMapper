// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Grid ZDir" {

	Properties
	{
		_GridThickness("Grid Thickness", Float) = 0.01
		_GridSpacing("Grid Spacing", Float) = 10.0
		_GridColour("Grid Colour", Color) = (1.0, 1.0, 1.0, 0.4)
		_BaseColour("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
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
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			// These are global properties and should not be instanced
			uniform float _BPMChange_Times[1963];
			uniform float _BPMChange_BPMs[1963];
			uniform int _BPMChange_Count;
			uniform float _Offset = 0;
			uniform float _Rotation = 0;
			uniform float _EditorScale = 4;

			// Define instanced properties
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _GridThickness)
				UNITY_DEFINE_INSTANCED_PROP(float, _GridSpacing)
				UNITY_DEFINE_INSTANCED_PROP(float4, _GridColour)
				UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
			UNITY_INSTANCING_BUFFER_END(Props)

			// Input into the vertex shader
			struct appdata {
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// Output from vertex shader into fragment shader
			struct v2f {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
				float4 rotatedPos : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// VERTEX SHADER
			v2f vert(appdata input)
			{
				v2f output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output); // necessary only if you want to access instanced properties in the fragment Shader.

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
			float4 frag(v2f input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);

				// Grab GPU Instanced parameters
				float gridSpacing = UNITY_ACCESS_INSTANCED_PROP(Props, _GridSpacing);
				float gridThickness = UNITY_ACCESS_INSTANCED_PROP(Props, _GridThickness);
				float4 gridColour = UNITY_ACCESS_INSTANCED_PROP(Props, _GridColour);
				float4 baseColour = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColour);

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
				if ((abs(time * editorScaleMult) % gridSpacing) / gridSpacing <= gridThickness / 2 || (abs(time * editorScaleMult) % gridSpacing) / gridSpacing >= 1 - (gridThickness / 2)) {
					return gridColour;
				} else {
					return baseColour;
				}
			}
			ENDCG
		}
	}
}
