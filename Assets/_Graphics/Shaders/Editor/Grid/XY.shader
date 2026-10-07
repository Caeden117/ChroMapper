Shader "ChroMapper/Editor/Grid/XY"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        
        _GridSpacing("Grid Spacing", Vector) = (1, 0.25, 0.125, 0.0625)
        _GridThickness("Grid Thickness", Vector) = (0.1, 0.05, 0.025, 0.0125)
        _GridOffset("Grid Offset", Vector) = (0, 0, 0, 0)
        _GridScale("Grid Scale", Range(0, 2)) = 1
        _LaneEdgeInset("Lane Edge Inset", Range(0, 0.5)) = 0
        _YEdgeInset("Y Edge Inset", Range(0, 0.5)) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        ZWrite Off
        Lighting Off
        // Clear the bloom mask only where grid pixels survive clip().
        Blend SrcAlpha OneMinusSrcAlpha, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../../ShaderLibrary/GridCoverage.hlsl"

            uniform float _Rotation = 0;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridSpacing)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridThickness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _LaneEdgeInset)
                UNITY_DEFINE_INSTANCED_PROP(float, _YEdgeInset)
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
                float2 localPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = _Rotation * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = o.worldPos.x * cos(rotationInRadians) - o.worldPos.z * sin(
                    rotationInRadians);
                float newZ = o.worldPos.z * cos(rotationInRadians) + o.worldPos.x * sin(
                    rotationInRadians);

                o.rotatedPos = float4(newX, o.worldPos.y, newZ, o.worldPos.w);
                o.localPos = v.vertex.xy;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 gridSpacing = UNITY_ACCESS_INSTANCED_PROP(Props, _GridSpacing);
                float4 gridThickness = UNITY_ACCESS_INSTANCED_PROP(Props, _GridThickness);
                float4 gridOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _GridOffset);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                float xPos = i.rotatedPos.x + gridOffset.x;
                float yPos = i.rotatedPos.y + gridOffset.y;
                float xFilter = max(fwidth(xPos), 1e-7);
                float yFilter = max(fwidth(yPos), 1e-7);

                // Mask the true wall bounds within the quad's overdraw margin.
                float laneEdge = 0.5 - UNITY_ACCESS_INSTANCED_PROP(Props, _LaneEdgeInset);
                float yEdge = 0.5 - UNITY_ACCESS_INSTANCED_PROP(Props, _YEdgeInset);
                float localXFilter = fwidth(i.localPos.x);
                float localYFilter = max(fwidth(i.localPos.y), 1e-7);
                float edgeMask = GridEdgeMask(i.localPos.x, laneEdge, 0.0, localXFilter);
                float yEdgeMask = GridEdgeMask(i.localPos.y, yEdge, 0.0, localYFilter);

                float coverage = 0;
                for (int idx = 0; idx < 4; idx++)
                {
                    float spacing = gridSpacing[idx];
                    if (spacing <= 0) continue;
                    float halfWidth = spacing * gridThickness[idx] * 0.5;
                    float plateau, ramp;
                    GridLineKernel(halfWidth, xFilter, plateau, ramp);
                    // Keep the outer AA ramp of vertical lines on the wall's side edges.
                    float xReach = (plateau + ramp) * localXFilter / xFilter;
                    float xMask = GridEdgeMask(i.localPos.x, laneEdge, xReach, localXFilter);
                    coverage = max(coverage, GridLineCoverage(xPos, spacing, halfWidth, xFilter)
                        * xMask * yEdgeMask);
                    coverage = max(coverage, GridLineCoverage(yPos, spacing, halfWidth, yFilter)
                        * edgeMask * yEdgeMask);
                }

                clip(coverage - 0.004);
                return half4(color.rgb, coverage);
            }
            ENDHLSL
        }
    }
}
