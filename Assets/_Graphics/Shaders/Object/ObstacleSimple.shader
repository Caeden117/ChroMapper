Shader "ChroMapper/Object/Obstacle Simple"
{
    Properties
    {
        _Color("Base Color", Color) = (0.5, 0, 0, 0)
        _WorldScale("World Scale", Vector) = (1, 3.5, 1, 1)

        [Header(Beat Saber)]
        [Space(10)]
        [Toggle(CUTOUT)] _EnableCutout("Enable Cutout", Float) = 0
        _CutoutTexScale("Cutout Texture Scale", Float) = 1
        _Cutout("Cutout", Range(0, 1)) = 0.0
        _CutoutTexOffset("Cutout Tex Offset", Vector) = (0, 0, 0, 0)

        [Header(Settings)] [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrc ("Blend Src", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDst ("Blend Dst", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeSrcA ("Blend Src A", float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendModeDstA ("Blend Dst A", float) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", float) = 0

        [Space]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", float) = 4
        [Toggle] _ZWrite ("Z Write", float) = 1
    }
    SubShader
    {
        Blend [_BlendModeSrc] [_BlendModeDst], [_BlendModeSrcA] [_BlendModeDstA]
        BlendOp [_BlendOp]
        Cull [_CullMode]
        ZTest [_ZTest]
        ZWrite [_ZWrite]

        Tags
        {
            "Queue"="Transparent+50"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment CUTOUT

            #include "UnityCG.cginc"
            #include "../ShaderLibrary/Core/Tonemapping.hlsl"

            uniform float _MainAlpha = 0.5;
            uniform sampler3D _CutoutTex;
            uniform float _CutoutTexScale;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WorldScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutout)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CutoutTexOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float4 cutoutPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex;
                o.uv = v.uv;
                o.cutoutPos = mul(unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                #if defined(CUTOUT)
                float cutout = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutout);
                float4 cutoutTexOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _CutoutTexOffset);
                float noise = tex3D(
                    _CutoutTex,
                    (i.cutoutPos + cutoutTexOffset.xyz) * _CutoutTexScale).a;
                clip(noise - cutout);
                #endif

                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                color.rgb *= 0.25;
                color.a = 0;

                color = ApplyAcesTonemapping(color);

                return color;
            }
            ENDHLSL
        }
    }
}