Shader "Unlit/GagaArc"
{
    Properties
    {
        // Properties for the lighting event.
        _MainTex ("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
        _EmissionColor("EmissionColor", 2D) = "white" {}
        _BaseColor("BaseColor", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _EmissionColor;
            float4 _BaseColor;
            
            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }
            
            float2 noise2d(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(hash(i + float2(0, 0)), hash(i + float2(1, 0)), u.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                float2 noise = noise2d(float2(v.vertex.x, _SinTime.y * 7.5)) * 2;
                v.vertex.z += noise.x;
                v.vertex.y += noise.y;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = col.rgb * _BaseColor.rgb;
                col.rgb += _EmissionColor.rgb * 0.5;
                return col;
            }
            ENDCG
        }
    }
}
