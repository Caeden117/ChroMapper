Shader "Grid Vertical"
{
    Properties
    {
        _GridThickness("Grid Thickness", Float) = 0.01
        _GridSpacing("Grid Spacing", Float) = 10.0
        _BaseColour("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
        _Directions("Direction", Vector) = (1.0, 0.0, 0.0)
        _BaseAlpha("Base Alpha", Float) = 0.075
        _GridAlpha("Grid Alpha", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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

            // These are global properties and should not be instanced
            uniform float _Rotation = 0;
            uniform float3 _Directions;

            // Define instanced properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridThickness)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridSpacing)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
                UNITY_DEFINE_INSTANCED_PROP(float, _BaseAlpha)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridAlpha)
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
                float4 worldPos : TEXCOORD0;
                float4 rotatedPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                //Global platform offset
                float4 offset = float4(0, -0.5, -1.5, 0);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = _Rotation * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = (o.worldPos.x - offset.x) * cos(rotationInRadians) - (o.worldPos.z - offset.z) * sin(rotationInRadians);
                float newZ = (o.worldPos.z - offset.z) * cos(rotationInRadians) + (o.worldPos.x - offset.x) * sin(rotationInRadians);

                o.rotatedPos = float4(newX, o.worldPos.y, newZ, o.worldPos.w);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Grab GPU Instanced parameters
                float gridSpacing = UNITY_ACCESS_INSTANCED_PROP(Props, _GridSpacing);
                float gridThickness = UNITY_ACCESS_INSTANCED_PROP(Props, _GridThickness);
                float4 baseColour = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColour);
                float baseAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseAlpha);
                float gridAlpha = UNITY_ACCESS_INSTANCED_PROP(Props, _GridAlpha);

                baseColour.a = baseAlpha;

                float xPos = i.rotatedPos.x;
                float yPos = i.rotatedPos.y * _Directions.y;

                if (_Directions.x != 0)
                {
                    if ((abs(xPos) % gridSpacing) / gridSpacing <= gridThickness / 2 || (abs(xPos) % gridSpacing) / gridSpacing >= 1 - (gridThickness / 2))
                    {
                        baseColour.a = gridAlpha;
                    }
                }

                if (_Directions.y != 0)
                {
                    if ((abs(yPos) % gridSpacing) / gridSpacing <= gridThickness / 2 || (abs(yPos) % gridSpacing) / gridSpacing >= 1 - (gridThickness / 2))
                    {
                        baseColour.a = gridAlpha;
                    }
                }

                return baseColour;
            }
            ENDCG
        }
    }
}
