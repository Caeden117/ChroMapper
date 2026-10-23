Shader "Hidden/Tests/VivifyProperties"
{
    Properties
    {
        _Value ("Value", Float) = 0.25
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            float4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
