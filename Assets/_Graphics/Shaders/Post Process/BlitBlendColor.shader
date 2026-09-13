Shader "Hidden/BlitBlendColor"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0)
    }

    HLSLINCLUDE
    struct AttributesDefault
    {
        uint vertexId : SV_VertexID;
    };

    struct VaryingsDefault
    {
        float4 vertex : SV_POSITION;
    };

    VaryingsDefault VertDefault(AttributesDefault input)
    {
        VaryingsDefault output;
        float2 uv = float2((input.vertexId << 1) & 2, input.vertexId & 2);
        output.vertex = float4(uv * 2.0 - 1.0, 0.0, 1.0);
        return output;
    }

    float4 _Color;

    float4 FragBlendColor(VaryingsDefault input) : SV_Target
    {
        return _Color;
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertDefault
            #pragma fragment FragBlendColor
            ENDHLSL
        }
    }
}