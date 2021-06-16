Shader "Unlit/Instanced Toon Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Outline("Outline width", Range(.002, 0.05)) = .005
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Opaque" }
        LOD 100
        Cull Front
        ZWrite Off
        ColorMask RGB
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
               UNITY_DEFINE_INSTANCED_PROP(fixed4, _OutlineColor)
               UNITY_DEFINE_INSTANCED_PROP(fixed, _Outline)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex + (v.vertex * UNITY_ACCESS_INSTANCED_PROP(Props, _Outline)));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                
                if (UNITY_ACCESS_INSTANCED_PROP(Props, _Outline) <= 0.01) clip(-1);

                return UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineColor);
            }
            ENDCG
        }
    }
}
