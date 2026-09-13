// ChroMapper replacement for Beat Saber Custom/SimpleStencil.
//
// AUDIT FINDINGS (1.44.3)
// S1. The authoritative 1.42.2 Properties block contains only the four stencil
//     and cull controls below. Lattice uses Ref=2; Lizzo uses Ref=1. Both use
//     Comp=Always, Pass=Replace, and back-face culling.
// S2 [vertex-e1db43b18c53b97e]: the no-keyword vertex route performs only the
//     object-to-world and world-to-clip transforms.
// S3 [vertex-ee83a5e328c2677c]: STEREO_INSTANCING_ON selects the stereo eye and
//     render-target array through Unity's standard instancing macros.
// S4 [fragment-8dc2c81abf29c14b,4a7230123d73103c]: both fragment routes output
//     float4(0,0,0,0). The white output in the 1.42.2 dummy export is incorrect.
// S5. The pass samples no textures and has no local feature keywords. Its color
//     blend preserves the destination, ZWrite is disabled, and only the stencil
//     operation has a visible effect.
// S6. Queue, blend, depth, cull, and stencil declarations are serialized pass
//     state and cannot be recovered from stage ASM. They remain the established
//     ChroMapper parity state; the material values support the stencil mapping.
Shader "ChroMapper/Stencil"
{
    Properties
    {
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass Op", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", Float) = 2
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1"
            "RenderType" = "Opaque"
        }
        Pass
        {
            Name ""
            Blend Zero One, Zero One
            ZClip On
            ZWrite Off
            Cull [_CullMode]

            Stencil
            {
                Ref [_StencilRefValue]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ STEREO_INSTANCING_ON

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 worldPos;
                worldPos = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
                worldPos += unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx;
                worldPos += unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz;
                worldPos += unity_ObjectToWorld._m03_m13_m23_m33;

                float4 clipPos;
                clipPos = worldPos.yyyy * unity_MatrixVP._m01_m11_m21_m31;
                clipPos += unity_MatrixVP._m00_m10_m20_m30 * worldPos.xxxx;
                clipPos += unity_MatrixVP._m02_m12_m22_m32 * worldPos.zzzz;
                clipPos += unity_MatrixVP._m03_m13_m23_m33 * worldPos.wwww;

                o.position = clipPos;
                return o;
            }

            struct fout
            {
                float4 sv_target : SV_Target;
            };

            fout frag(v2f inp)
            {
                fout o;
                o.sv_target = float4(0.0, 0.0, 0.0, 0.0);
                return o;
            }
            ENDCG
        }
    }
}