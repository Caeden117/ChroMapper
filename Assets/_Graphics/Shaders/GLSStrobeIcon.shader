Shader "ChroMapper/GLS Strobe Icon"
{
    Properties
    {
        [NoScaleOffset] _SdfAtlas ("Strobe Distance Fields", 2D) = "white" {}
        [HideInInspector] _SdfLayout ("Atlas Layout", Vector) = (75,36,24,23)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup

            #include "UnityCG.cginc"
            sampler2D _SdfAtlas;
            float4 _SdfAtlas_TexelSize;
            float4 _SdfLayout;

            struct IconInstance
            {
                float4x4 transform;
                float4 curve;
                float4 atlas;
                float4 cornerAtlas;
            };
            StructuredBuffer<IconInstance> _Icons;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 coordinate : TEXCOORD0;
                nointerpolation float4 curve : TEXCOORD1;
                nointerpolation float4 atlas : TEXCOORD2;
                nointerpolation float2 fallingOrigin : TEXCOORD3;
                nointerpolation float4 cornerOrigins : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            void Setup()
            {
            }

            v2f vert(appdata v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    IconInstance icon = _Icons[unity_InstanceID];
                #else
                    IconInstance icon = _Icons[0];
                #endif
                o.vertex = mul(UNITY_MATRIX_VP, mul(icon.transform, v.vertex));
                o.coordinate = v.uv * float2(284, 128);
                o.curve = icon.curve;
                o.atlas = icon.atlas;
                o.cornerOrigins = icon.cornerAtlas;
                int2 cell = (int2)round((icon.atlas.xy - 2) / _SdfLayout.xy);
                int referenceIndex = (cell.y * (int)_SdfLayout.z + cell.x) % (int)_SdfLayout.w;
                int fallingField = ((int)icon.curve.x * (int)_SdfLayout.w) + (int)_SdfLayout.w - 1 - referenceIndex;
                o.fallingOrigin = float2(fallingField % (int)_SdfLayout.z, fallingField / (int)_SdfLayout.z) * _SdfLayout.xy + 2;
                return o;
            }

            float2 CurvePoint(float phase, float value, float4 curve)
            {
                return float2(13 + (258 * phase), 13 + (102 * (value - curve.z) / curve.w));
            }

            float2 SegmentOffset(float2 coordinate, float2 start, float2 end)
            {
                float2 delta = end - start;
                float position = saturate(dot(coordinate - start, delta) / max(dot(delta, delta), 1e-12));
                return coordinate - start - (position * delta);
            }

            float3 OffsetDistance(float2 offset)
            {
                float distance = length(offset);
                return float3(distance, offset / max(distance, 1e-4));
            }

            float3 CloserDistance(float3 first, float3 second)
            {
                return first.x <= second.x ? first : second;
            }

            float SampleDistance(float2 coordinate, float2 origin)
            {
                float2 source = clamp(coordinate, float2(-6, -6), (_SdfLayout.xy * 4) - 10);
                float2 uv = (origin + (source * 0.25)) * _SdfAtlas_TexelSize.xy;
                float encoded = tex2Dlod(_SdfAtlas, float4(uv, 0, 0)).r;
                return ((encoded - 0.5) * 32) + 5.90625;
            }

            float3 CorrectDistance(float center, float2 gradient, float inverseScale, bool falling)
            {
                float2 transformed = gradient * float2(inverseScale, falling ? -1 : 1);
                float gradientLength = length(transformed);
                float distance = max(center, 0) * max(length(gradient), 1e-4) / max(gradientLength, 1e-4);
                return float3(distance, transformed / max(gradientLength, 1e-4));
            }

            float3 FieldDistance(float2 source, float2 origin, float inverseScale, bool falling)
            {
                float2 sampled = clamp(source, float2(-6, -6), (_SdfLayout.xy * 4) - 10);
                float center = SampleDistance(sampled, origin);
                float2 gradient = float2(
                    SampleDistance(sampled + float2(4, 0), origin) - SampleDistance(sampled - float2(4, 0), origin),
                    SampleDistance(sampled + float2(0, 4), origin) - SampleDistance(sampled - float2(0, 4), origin)) / 8;
                center += dot(gradient, source - sampled);
                return CorrectDistance(center, gradient, inverseScale, falling);
            }

            float3 PhaseDistance(float2 coordinate, float start, float duration, float referenceDuration,
                bool falling, float4 curve, float2 origin, float2 cornerOrigin, float padding)
            {
                if (duration == 0)
                {
                    return OffsetDistance(SegmentOffset(coordinate, CurvePoint(start, 0, curve), CurvePoint(start, 1, curve)));
                }

                if (coordinate.x < 13 + (258 * start) - padding || coordinate.x > 13 + (258 * (start + duration)) + padding)
                {
                    return float3(1e5, 1, 0);
                }

                float inverseScale = referenceDuration / duration;
                // Both phases use rising fields. Reflection around y=64 gives the falling phase with the same overshoot range.
                float2 source = float2(13 + ((coordinate.x - 13 - (258 * start)) * inverseScale), falling ? 128 - coordinate.y : coordinate.y);
                float3 closest = FieldDistance(source, origin, inverseScale, falling);
                // Sample the two curve fields separately before choosing the nearer one, preserving Bounce
                // corners and concave outlines.
                if (cornerOrigin.x >= 0)
                {
                    closest = CloserDistance(closest, FieldDistance(source, cornerOrigin, inverseScale, falling));
                }

                return closest;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 horizontalPixel = ddx(i.coordinate);
                float2 verticalPixel = ddy(i.coordinate);
                float phasePadding = 11.25 + (0.5 * (abs(horizontalPixel.x) + abs(verticalPixel.x)));
                float pulseWidth = i.curve.y;
                float3 closest;
                if ((int)i.curve.x == 31)
                {
                    float2 bottomStart = CurvePoint(0, 0, i.curve);
                    float2 bottomSplit = CurvePoint(pulseWidth, 0, i.curve);
                    float2 topSplit = CurvePoint(pulseWidth, 1, i.curve);
                    float2 topEnd = CurvePoint(1, 1, i.curve);
                    closest = CloserDistance(
                        OffsetDistance(SegmentOffset(i.coordinate, bottomStart, bottomSplit)),
                        OffsetDistance(SegmentOffset(i.coordinate, bottomSplit, topSplit)));
                    closest = CloserDistance(closest, OffsetDistance(SegmentOffset(i.coordinate, topSplit, topEnd)));
                    closest = CloserDistance(closest, OffsetDistance(SegmentOffset(i.coordinate, topEnd, CurvePoint(1, 0, i.curve))));
                }
                else
                {
                    closest = CloserDistance(
                        PhaseDistance(i.coordinate, 0, pulseWidth, i.atlas.z, false, i.curve, i.atlas.xy, i.cornerOrigins.xy, phasePadding),
                        PhaseDistance(i.coordinate, pulseWidth, 1 - pulseWidth, 1 - i.atlas.z, true, i.curve, i.fallingOrigin, i.cornerOrigins.zw, phasePadding));
                    closest = CloserDistance(closest, OffsetDistance(SegmentOffset(i.coordinate,
                        CurvePoint(pulseWidth, i.atlas.w, i.curve), CurvePoint(pulseWidth, 1, i.curve))));
                }

                float antialias = max(abs(dot(closest.yz, horizontalPixel)) + abs(dot(closest.yz, verticalPixel)), 1e-4);
                float coverage = saturate(((11.25 - closest.x) / antialias) + 0.5);
                float white = saturate(((5.90625 - closest.x) / antialias) + 0.5);
                clip(coverage - 0.02);
                return half4(white.xxx, coverage);
            }
            ENDHLSL
        }
    }
}
