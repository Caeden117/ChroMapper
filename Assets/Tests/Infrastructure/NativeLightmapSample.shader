Shader "Hidden/Tests/NativeLightmapSample"
{
    Properties
    {
        _Lightmap ("Lightmap", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _Lightmap;
            float4 _Sample;
            float4 frag(v2f_img input) : SV_Target
            {
                return tex2Dlod(_Lightmap, _Sample);
            }
            ENDCG
        }
    }
}
