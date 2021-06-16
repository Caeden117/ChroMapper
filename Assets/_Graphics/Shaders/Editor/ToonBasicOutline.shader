//Shader courtesy of Unity
Shader "Toon/Basic Outline" 
{
	Properties 
	{
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.05)) = .005
	}
	SubShader 
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" }
		
		Cull Front
        ZWrite Off
        ColorMask RGB
        Blend SrcAlpha OneMinusSrcAlpha
		
		Pass 
		{
			Name "OUTLINE"
			
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
            #pragma multi_compile_instancing
			
            CBUFFER_START(UnityPerMaterial)
            float _Outline;
            float4 _OutlineColor;
            CBUFFER_END
			
            struct Attributes 
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
        
            struct Varyings 
            {
                float4 positionCS : SV_POSITION;
                half fogCoord : TEXCOORD0;
                half4 color : COLOR;
            };
            
            Varyings vert(Attributes input) 
            {
                Varyings output = (Varyings)0;
                
                if (_Outline > 0.01)
                {
                    input.positionOS.xyz += input.positionOS * _Outline;

                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    output.positionCS = vertexInput.positionCS;

                    output.color = _OutlineColor;
                    output.fogCoord = ComputeFogFactor(output.positionCS.z);
                }
                return output;
            }
			
            half4 frag(Varyings i) : SV_Target
            {
                if (_Outline <= 0.01) clip(-1);
				i.color.rgb = MixFog(i.color.rgb, i.fogCoord);
				return i.color;
			}
            ENDHLSL
		}
	}
}
