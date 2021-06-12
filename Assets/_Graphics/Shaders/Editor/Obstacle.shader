Shader "Unlit/Obstacle"
{
    Properties
    {
        _ColorTint("Base Color", Color) = (0.5, 0, 0, 0)
        _FadeSize("Fade Size", Float) = 1
        _MainAlpha("Main Alpha", Float) = 1
        _Rotation("Rotation", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Back
        ZTest LEqual
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            // These are global properties and should not be instanced
            uniform float _EditorScale = 4;
            uniform float _OutsideAlpha = 1;

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _MainAlpha)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorTint)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float4 rotatedPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float magnitude(float4 v)
            {
                return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            }

            float4 normalize(float4 v)
            {
                return v * (1 / magnitude(v));
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.pos = UnityObjectToClipPos(v.vertex);

                // Calculate the world position coordinates to pass to the fragment shader
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                //Global platform offset
                float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = UNITY_ACCESS_INSTANCED_PROP(Props, _Rotation) * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = (o.worldPos.x - offset.x) * cos(rotationInRadians) - (o.worldPos.z - offset.z) * sin(rotationInRadians);
                float newZ = (o.worldPos.z - offset.z) * cos(rotationInRadians) + (o.worldPos.x - offset.x) * sin(rotationInRadians);

                o.rotatedPos = float4(newX + offset.x, o.worldPos.y, newZ + offset.z, o.worldPos.w);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorTint);
                float mainAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _MainAlpha);

                float mag = magnitude(color);
                float4 normal = normalize(color);

                if (mag > magnitude(normal))
                {
                    color = normal * sqrt(mag);
                }

                float circleRadius = _EditorScale * 2;
                float fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);

                float distance = abs(i.rotatedPos.z);

                float t = (distance - circleRadius) / fadeSize;

                if (distance < circleRadius + fadeSize && distance > circleRadius)
                {
                    return float4(color.r, color.g, color.b, lerp(mainAlpha, _OutsideAlpha, t));
                }
                else if (distance > circleRadius + fadeSize)
                {
                    return float4(color.r, color.g, color.b, _OutsideAlpha);
                }
                else
                {
                    return float4(color.r, color.g, color.b, mainAlpha);
                }
            }
            ENDCG
        }
    }
}
