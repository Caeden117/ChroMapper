Shader "ChroMapper/BloomfogMesh"
{
    // Material properties control textures and blending. Runtime fog values remain globals.
    // The cubic RGB transfer preserves vertex alpha. BLOOM_FOG alone enables attenuation.
    // The procedural mesh is two-sided and uses dynamic blending without depth writes.
    // The OVERDRAW_VIEW debug route is intentionally omitted.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Space] [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactor ("Blend Src Factor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactor ("Blend Dst Factor", Float) = 10
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrcFactorA ("Blend Src Factor A", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDstFactorA ("Blend Dst Factor A", Float) = 10
        [Space] [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        ZWrite Off
        Cull Off
        Blend [_BlendSrcFactor] [_BlendDstFactor], [_BlendSrcFactorA] [_BlendDstFactorA]
        BlendOp [_BlendOp]
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ BLOOM_FOG

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float3 tangent : TANGENT;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float3 uv : TEXCOORD0;
            };

            uniform float4x4 _VertexTransformMatrix;
            uniform float _CustomFogOffset;
            uniform float _CustomFogAttenuation;

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                // Set once as a global by BloomfogRendererSO.Initialize to match the game's runtime cbuffer matrix
                o.vertex = mul(_VertexTransformMatrix, float4(v.vertex, 1.0));
                o.uv = v.uv;

                float4 color = v.color;
                // Recovered GammaToLinearSpace cubic transfer; alpha is untouched.
                color.rgb = color.rgb * (color.rgb * (color.rgb * 0.305306011
                    + 0.682171111) + 0.012522878);
                o.color = color;

                o.tangent.xyz = v.tangent / v.tangent.z;
                o.tangent.w = 1.0 / v.tangent.z;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 dir = i.tangent.xyz / i.tangent.w;

                float dir2 = dot(dir.xyz, dir.xyz);

                float u0 = 1.0;
                #if defined(BLOOM_FOG)
                float alpha = 1.0 / max(i.color.a, 1.0);
                u0 = max(dir2 * alpha - _CustomFogOffset, 0.0);
                u0 = 1.0 / (u0 * _CustomFogAttenuation + 1.0);
                #endif

                float2 uv = float2(i.uv.x / i.uv.z, i.uv.y);

                // sample generated here, but sample_indexable in DXBC
                // this will be functionally equivalent
                float4 line_mask = tex2D(_MainTex, uv);

                float a2 = i.color.a * i.color.a;

                float4 color = float4(i.color.rgb, a2);

                float4 bloom_color = line_mask * color;

                float bloom_alpha = u0 * bloom_color.a;

                return float4(bloom_color.rgb * bloom_alpha, bloom_alpha);
            }
            ENDHLSL
        }
    }
}
