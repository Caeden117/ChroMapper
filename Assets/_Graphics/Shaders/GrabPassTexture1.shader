Shader "Custom/GrabPassTexture1"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        GrabPass
        {
            "_GrabTexture1"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            Varyings vert(appdata_base input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
}