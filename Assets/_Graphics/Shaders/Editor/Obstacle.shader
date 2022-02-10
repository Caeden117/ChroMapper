Shader "Unlit/Obstacle"
{
    Properties
    {
        _ColorTint("Base Color", Color) = (0.5, 0, 0, 0)
        _FadeSize("Fade Size", Float) = 1
        _MainAlpha("Main Alpha", Float) = 1
        _Rotation("Rotation", Float) = 0
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
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
            uniform float _OutsideAlpha = 1;
            uniform float _ObstacleFadeRadius = 8;

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _MainAlpha)
                UNITY_DEFINE_INSTANCED_PROP(float, _Rotation)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 worldPos : TEXCOORD1;
                float4 rotatedPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = v.normal;

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

                /// Outline ///
                float4 worldScale = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldScale));
                float uvXScalar = 0;
                float uvYScalar = 0;

                if (i.normal.x != 0)
                {
                    uvXScalar = worldScale.z;
                    uvYScalar = worldScale.y;
                }
                else if (i.normal.y != 0)
                {
                    uvYScalar = worldScale.z;
                    uvXScalar = worldScale.x;
                }
                else
                {
                    uvXScalar = worldScale.x;
                    uvYScalar = worldScale.y;
                }


                float mainAlpha = 0;

                float2 halfUv = 0.5 - abs(0.5 - i.uv);
                if ((halfUv.x * uvXScalar) < 0.075 || (halfUv.y * uvYScalar) < 0.075)
                {
                    mainAlpha = 1;
                }
                else
                {
                    mainAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _MainAlpha);
                }

                /// Coloring ///
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorTint);

                float mag = length(color);

                if (mag > 1)
                {
                    color = normalize(color) * sqrt(mag);
                }

                float fadeSize = UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize);
                float circleRadius = _ObstacleFadeRadius - fadeSize;

                float distance = abs(i.rotatedPos.z);

                float t = clamp((distance - circleRadius) / fadeSize, 0, 1);

                if (_OutsideAlpha > 0)
                {
                    return float4(color.rgb, mainAlpha);
                }
                else
                {
                    return float4(color.rgb, lerp(mainAlpha, _OutsideAlpha, t));
                }
            }
            ENDCG
        }
    }
}
