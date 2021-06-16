// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Event Shader"
{
    Properties
    {
        _ColorTint("Color Tint", Color) = (1, 0, 0, 0)
        _ColorBase("Base Color", Color) = (0, 0, 0, 0)
        _Position("Point Position", Vector) = (0, 0, 0, 0)
        _CircleRadius("Spotlight Size", Float) = 0.2
        _FadeSize("Fade Size", Float) = 0.5
        _MainAlpha("Base Alpha", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
               UNITY_DEFINE_INSTANCED_PROP(fixed4, _ColorTint)
               UNITY_DEFINE_INSTANCED_PROP(fixed4, _ColorBase)
               UNITY_DEFINE_INSTANCED_PROP(fixed4, _Position)
               UNITY_DEFINE_INSTANCED_PROP(fixed, _CircleRadius)
               UNITY_DEFINE_INSTANCED_PROP(fixed, _FadeSize)
               UNITY_DEFINE_INSTANCED_PROP(fixed, _MainAlpha)
            UNITY_INSTANCING_BUFFER_END(Props)

            // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float4 vertex : POSITION0; // clip space position
                float4 vertex_Object : POSITION1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex_Object = v.vertex;
                return o;
            }

            // pixel shader, no inputs needed
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

                fixed4 position = UNITY_ACCESS_INSTANCED_PROP(Props, _Position);
                fixed4 colorTint = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorTint);
                fixed4 colorBase = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorBase);
                fixed circleRadius = UNITY_ACCESS_INSTANCED_PROP(Props, _CircleRadius);
                fixed fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);
                fixed mainAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _MainAlpha);

                fixed distance = abs(i.vertex_Object.z - position.z);

                fixed t = (distance - circleRadius) / fadeSize;

                if (distance < circleRadius + fadeSize && distance > circleRadius)
                {
                    fixed4 transitionColor = lerp(colorTint, colorBase, t);

                    return transitionColor;
                }
                else if (distance > circleRadius + fadeSize)
                {
                    return colorBase;
                }
                else
                {
                    return colorTint;
                }
            }
            ENDCG
        }
    }
}
