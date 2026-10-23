Shader "Hidden/Tests/NativeCubemapSample"
{
    Properties { _Probe ("Probe", Cube) = "" {} }
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            samplerCUBE _Probe;
            float4 _Direction;
            float _Mip;
            float4 frag(v2f_img input) : SV_Target
            {
                return float4(texCUBElod(_Probe, float4(_Direction.xyz, _Mip)).rgb, 1);
            }
            ENDHLSL
        }
    }
}
