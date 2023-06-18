Shader "Basic Gradient"
{
    Properties
    {
        _ColorA("Color A", Color) = (1.0, 0.0, 0.0, 1.0)
        _ColorB("Color B", Color) = (1.0, 0.0, 0.0, 1.0)
        _EasingID("Easing ID", Int) = 0
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "Shared/Easings.cginc"

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                UNITY_DEFINE_INSTANCED_PROP(int, _EasingID)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Grab GPU Instanced parameters
                float4 a = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA);
                float4 b = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB);
                float t = i.uv.x;

                int id = UNITY_ACCESS_INSTANCED_PROP(Props, _EasingID);

                // a small price to pay for salvation
                switch (id) {
                    case 1:
                        t = Quadratic_In(t);
                        break;
                    case 2:
                        t = Quadratic_Out(t);
                        break;
                    case 3:
                        t = Quadratic_InOut(t);
                        break;
                    case 4:
                        t = Cubic_In(t);
                        break;
                    case 5:
                        t = Cubic_Out(t);
                        break;
                    case 6:
                        t = Cubic_InOut(t);
                        break;
                    case 7:
                        t = Quartic_In(t);
                        break;
                    case 8:
                        t = Quartic_Out(t);
                        break;
                    case 9:
                        t = Quartic_InOut(t);
                        break;
                    case 10:
                        t = Quintic_In(t);
                        break;
                    case 11:
                        t = Quintic_Out(t);
                        break;
                    case 12:
                        t = Quintic_InOut(t);
                        break;
                    case 13:
                        t = Sinusoidal_In(t);
                        break;
                    case 14:
                        t = Sinusoidal_Out(t);
                        break;
                    case 15:
                        t = Sinusoidal_InOut(t);
                        break;
                    case 16:
                        t = Exponential_In(t);
                        break;
                    case 17:
                        t = Exponential_Out(t);
                        break;
                    case 18:
                        t = Exponential_InOut(t);
                        break;
                    case 19:
                        t = Circular_In(t);
                        break;
                    case 20:
                        t = Circular_Out(t);
                        break;
                    case 21:
                        t = Circular_InOut(t);
                        break;
                    case 22:
                        t = Elastic_In(t);
                        break;
                    case 23:
                        t = Elastic_Out(t);
                        break;
                    case 24:
                        t = Elastic_InOut(t);
                        break;
                    case 25:
                        t = Back_In(t);
                        break;
                    case 26:
                        t = Back_Out(t);
                        break;
                    case 27:
                        t = Back_InOut(t);
                        break;
                    case 28:
                        t = Bounce_In(t);
                        break;
                    case 29:
                        t = Bounce_Out(t);
                        break;
                    case 30:
                        t = Bounce_InOut(t);
                        break;
                    case 31:
                        t = Step(t);
                        break;
                }

                float4 color = lerp(a, b, t);

                float mult = max(color.a, 1);
                color.r *= mult;
                color.g *= mult;
                color.b *= mult;

                color.a = clamp(color.a, 0, 1);

                return color;
            }
            ENDCG
        }
    }
}
