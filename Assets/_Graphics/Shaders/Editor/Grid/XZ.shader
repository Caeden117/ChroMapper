Shader "ChroMapper/Editor/Grid/XZ"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)

        _GridSpacing("Grid Spacing", Vector) = (1, 0.25, 0.125, 0.0625)
        _GridThickness("Grid Thickness", Vector) = (0.1, 0.05, 0.025, 0.0125)
        _GridOffset("Grid Offset", Vector) = (0, 0, 0, 0)
        _GridScale("Grid Scale", Range(0, 2)) = 1
        _LaneEdgeInset("Lane Edge Inset", Range(0, 0.5)) = 0
        _ZEdgeInset("Z Edge Inset", Range(0, 0.5)) = 0
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

            uniform float _SongBPM = 120;
            uniform float _SongTimeOrigin = 0;
            uniform float _BPMChange_Times[170];
            uniform float _BPMChange_Json_Times[170];
            uniform float _BPMChange_BPMs[170];
            uniform int _BPMChange_Count;
            uniform float4 _SongBpmTime;
            uniform float _Rotation = 0;
            uniform float _EditorScale = 4;
            uniform float _CurrentHJD = 2;
            uniform int _DisplayHJDLine = 1;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridSpacing)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridThickness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _GridOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _GridScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _LaneEdgeInset)
                UNITY_DEFINE_INSTANCED_PROP(float, _ZEdgeInset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 rotatedPos : TEXCOORD0;
                float2 quadPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                //Get rotation in radians (this is used for 360/90 degree map rotation).
                float rotationInRadians = _Rotation * (3.141592653 / 180);

                //Transform X and Z around global platform offset (2D rotation PogU)
                float newX = worldPos.x * cos(rotationInRadians) - worldPos.z * sin(rotationInRadians);
                float newZ = worldPos.z * cos(rotationInRadians) + worldPos.x * sin(rotationInRadians);

                o.rotatedPos = float3(newX, worldPos.y, newZ);
                o.quadPos = v.vertex.xy;

                return o;
            }

            // Converts a song BPM time to JSON time, accounting for BPM changes.
            float songBpmTimeToJsonTime(float songBpmTime)
            {
                if (songBpmTime < 0) return songBpmTime;

                // Walk backwards to find the BPM region containing songBpmTime
                // we could also walk forwards but walking backwards is slightly simpler
                // truthfully idk if this is faster but i sure like how simpler it is
                for (int bpmIdx = _BPMChange_Count - 1; bpmIdx >= 0; bpmIdx--)
                {
                    float regionStart = _BPMChange_Times[bpmIdx];
                    if (regionStart <= songBpmTime)
                    {
                        float localBeats = (songBpmTime - regionStart) * _BPMChange_BPMs[bpmIdx] / _SongBPM;
                        return localBeats + _BPMChange_Json_Times[bpmIdx];
                    }
                }

                return songBpmTime;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 gridSpacing = UNITY_ACCESS_INSTANCED_PROP(Props, _GridSpacing);
                float4 gridThickness = UNITY_ACCESS_INSTANCED_PROP(Props, _GridThickness);
                float4 gridOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _GridOffset);
                float gridScale = UNITY_ACCESS_INSTANCED_PROP(Props, _GridScale);
                half4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                // Mask the true track bounds within the quad's overdraw margin.
                float laneEdge = 0.5 - UNITY_ACCESS_INSTANCED_PROP(Props, _LaneEdgeInset);
                float zEdge = 0.5 - UNITY_ACCESS_INSTANCED_PROP(Props, _ZEdgeInset);
                float localXFilter = fwidth(i.quadPos.x);
                float localZFilter = fwidth(i.quadPos.y);
                float edgeMask = GridEdgeMask(i.quadPos.x, laneEdge, 0.0, localXFilter);
                float zEdgeMask = GridEdgeMask(i.quadPos.y, zEdge, 0.0, localZFilter);

                float scale = _EditorScale * gridScale;
                //WHERE'S THE LAMB SAUCE (unedited beat time)
                float timeButRAWWW = (i.rotatedPos.z + gridOffset.z + _SongBpmTime.y * scale) / scale;

                //To plugerino into shader after dealing with BPM Changes
                float time = songBpmTimeToJsonTime(timeButRAWWW);

                // Apply visual beat origin offset (precomputed as JSON time on CPU)
                time -= _SongTimeOrigin;

                // Keep HJD relative to the cursor in song-BPM time, before BPM changes and origin offsets.
                float timeOffsetToCursor = timeButRAWWW - _SongBpmTime.y;
                float timeFilter = max(fwidth(timeButRAWWW), 1e-7);
                float hjdRange = gridThickness.x * 0.34;
                float hjdCoverage = _DisplayHJDLine
                    ? GridLineCoverageAtDistance(abs(timeOffsetToCursor - _CurrentHJD), hjdRange, timeFilter)
                    : 0;
                hjdCoverage *= edgeMask * zEdgeMask;

                float t = time * scale / _EditorScale;
                float tFilter = max(fwidth(t), 1e-7);
                float coverage = 0;
                for (int idx = 0; idx < 4; idx++)
                {
                    float spacing = gridSpacing[idx];
                    if (spacing <= 0) continue;
                    float halfWidth = spacing * gridThickness[idx] * 0.5;
                    coverage = max(coverage, GridLineCoverage(t, spacing, halfWidth, tFilter)
                        * edgeMask * zEdgeMask);
                }

                // Keep lane lines' outer AA ramps at the side edges, but stop their ends at the track bounds.
                if (gridScale > 0)
                {
                    float laneHalfWidth = 0.05 * gridScale * gridScale;
                    float laneFilter = max(fwidth(i.rotatedPos.x), 1e-7);
                    float lanePlateau, laneRamp;
                    GridLineKernel(laneHalfWidth, laneFilter, lanePlateau, laneRamp);
                    float laneReach = (lanePlateau + laneRamp) * localXFilter / laneFilter;
                    coverage = max(coverage, GridLineCoverage(
                        i.rotatedPos.x + gridOffset.x, gridScale,
                        laneHalfWidth, laneFilter)
                        * GridEdgeMask(i.quadPos.x, laneEdge, laneReach, localXFilter)
                        * zEdgeMask);
                }

                // Keep the grid visible beneath the HJD line's faded edges.
                coverage = max(coverage, hjdCoverage);
                clip(coverage - 0.004);
                return half4(lerp(color.rgb, half3(0.5, 0, 0), hjdCoverage), coverage);
            }
            ENDHLSL
        }
    }
}
