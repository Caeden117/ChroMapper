Shader "ChroMapper/Object/Basic Gradient"
{
    Properties
    {
        _ColorA("Color A", Color) = (1.0, 0.0, 0.0, 1.0)
        _ColorB("Color B", Color) = (1.0, 0.0, 0.0, 1.0)
        _EasingID("Easing ID", Int) = 0
        _UseHSV("Use HSV", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcColor OneMinusSrcColor

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Easings.hlsl"
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                UNITY_DEFINE_INSTANCED_PROP(int, _EasingID)
                UNITY_DEFINE_INSTANCED_PROP(int, _UseHSV)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Keep the ribbon's HSV conversion self-contained because the shared shader includes only easing functions.
            float3 RGBToHSV(float3 color)
            {
                const float epsilon = 1e-10f;
                const float4 constants = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp(float4(color.bg, constants.wz), float4(color.gb, constants.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float chroma = q.x - min(q.w, q.y);
                return float3(abs(q.z + ((q.w - q.y) / ((6.0f * chroma) + epsilon))), chroma / (q.x + epsilon), q.x);
            }

            // Convert the hue, saturation, and value interpolated above back to the display color.
            float3 HSVToRGB(float3 color)
            {
                float3 rgb = abs((frac(color.xxx + float3(0.0f, 2.0f / 3.0f, 1.0f / 3.0f)) * 6.0f) - 3.0f);
                return color.z * lerp(1.0f, saturate(rgb - 1.0f), color.y);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Grab GPU Instanced parameters
                float4 startColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA);
                float4 endColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB);
                float t = i.uv.x;

                int id = UNITY_ACCESS_INSTANCED_PROP(Props, _EasingID);

                // a small price to pay for salvation
                switch (id)
                {
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
                default:
                    break;
                }

                // Match BasicEventColorLerp: mode 1 preserves legacy scalar HSV and mode 2 uses shortest-path trueHSV.
                float4 color;
                int colorLerpType = UNITY_ACCESS_INSTANCED_PROP(Props, _UseHSV);
                if (colorLerpType != 0)
                {
                    float4 startHsv = float4(RGBToHSV(startColor.rgb), startColor.a);
                    float4 endHsv = float4(RGBToHSV(endColor.rgb), endColor.a);
                    float hue;
                    if (colorLerpType == 1)
                    {
                        // Existing HSV data linearly interpolates normalized hue even when that takes the long arc through green.
                        hue = lerp(startHsv.x, endHsv.x, t);
                    }
                    else
                    {
                        // trueHSV mirrors Mathf.LerpAngle's shortest signed delta and hue-only easing clamp.
                        float hueDelta = frac(endHsv.x - startHsv.x);
                        if (hueDelta > 0.5f) hueDelta -= 1.0f;
                        hue = frac(startHsv.x + (hueDelta * saturate(t)));
                    }

                    float3 hsv = float3(
                        hue,
                        lerp(startHsv.y, endHsv.y, t),
                        lerp(startHsv.z, endHsv.z, t));
                    color = float4(HSVToRGB(hsv), lerp(startHsv.a, endHsv.a, t));
                }
                else
                {
                    color = lerp(startColor, endColor, t);
                }

                float mult = max(color.a, 1);
                color.r *= mult;
                color.g *= mult;
                color.b *= mult;

                color.rgb *= clamp(color.a, 0, 1);
                color.a = 0;

                color = ApplyAcesTonemapping(color);
                return color;
            }
            ENDHLSL
        }
    }
}