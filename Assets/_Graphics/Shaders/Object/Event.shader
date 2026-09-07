// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ChroMapper/Object/Event"
{
    Properties
    {
        _ColorA("Color A", Color) = (0, 0, 0, 0)
        _ColorB("Color B", Color) = (1, 0, 0, 0)
        _FadeSize("Fade Size", float) = 0.5
        _Offset("Offset", float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorA)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorB)
                UNITY_DEFINE_INSTANCED_PROP(float, _FadeSize)
                UNITY_DEFINE_INSTANCED_PROP(float, _Offset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : POSITION0; // clip space position
                float4 localPos : POSITION1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 colorA = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorA);
                float4 colorB = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorB);
                float fadeSize = abs(UNITY_ACCESS_INSTANCED_PROP(Props, _FadeSize));
                float offset = UNITY_ACCESS_INSTANCED_PROP(Props, _Offset);

                colorA.a = 0;
                colorB.a = 0;

                float pos = i.localPos.z + offset;

                float4 col;
                if (abs(pos) < fadeSize) col = lerp(colorA, colorB, saturate((pos + fadeSize / 2) / fadeSize));
                else if (pos >= fadeSize) col = colorB;
                else col = colorA;

                col = ApplyAcesTonemapping(col);
                return col;
            }
            ENDHLSL
        }
    }
}