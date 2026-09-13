Shader "ChroMapper/Sky Gradient"
{
    Properties
    {
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Background" "Queue"="Background"
        }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fragment _ USE_TONE_MAPPING
            #pragma multi_compile_fragment _ ACES_TONE_MAPPING

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            uniform float4x4 _InverseProjectionMatrix;
            uniform float4x4 _CameraToWorldMatrix;
            sampler2D _GradientTex;
            float4 _Color;

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                // SG4: recovered vertices 7a39dcc4 / ff68e24d build the fullscreen
                // triangle from SV_VertexID ((id<<1)&2, id&2 -> NDC (-1,-1),(3,-1),
                // (-1,3)) with explicit zw = (1, 1). The blit mesh is ignored.
                // id % 3 folds Graphics.Blit's fourth quad vertex onto vertex 0 so
                // the quad's second triangle degenerates instead of additively
                // double-covering part of the screen.
                uint corner = id % 3;
                float2 tri = float2((corner << 1) & 2, corner & 2);
                o.vertex = float4(tri * 2.0 - 1.0, 1.0, 1.0);

                // SG4: the game unprojects with NEGATED screen Y
                // (r0.z = 1 - tri.y => -ndc.y) and z = w = 1.
                float4 clipPosition = float4(o.vertex.x, -o.vertex.y, 1.0, 1.0);
                float4 viewPosition = mul(_InverseProjectionMatrix, clipPosition);

                // SG4: rotation-only transform (translation column never read),
                // no w-divide — the fragment normalizes, so the uniform scale
                // in viewPosition.w cancels exactly.
                o.direction = mul((float3x3)_CameraToWorldMatrix, viewPosition.xyz);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // SG4: recovered fragment c37b9c30 — t = normalize(ray).y * 0.5 + 0.5,
                // sampled at (t, 0), multiplied by _Color.
                float gradientCoordinate = normalize(i.direction).y * 0.5 + 0.5;
                half4 color = tex2D(_GradientTex, float2(gradientCoordinate, 0.0)) * _Color;

                #if defined(USE_TONE_MAPPING) && defined(ACES_TONE_MAPPING)
                color = ApplyAcesTonemapping(color);
                #endif
                return color;
            }
            ENDHLSL
        }
    }
}