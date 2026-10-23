Shader "Hidden/ChroMapper/VivifyDepthCopy"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_VivifyCaptureDepthTexture);

            float frag(v2f_img input) : SV_Target
            {
                return SAMPLE_DEPTH_TEXTURE(_VivifyCaptureDepthTexture, input.uv);
            }
            ENDCG
        }
    }
    Fallback Off
}
