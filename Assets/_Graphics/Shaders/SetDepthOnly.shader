// Replacement for the Beat Saber game shader Custom/SetDepthOnly.
Shader "ChroMapper/Set Depth Only"
{
    // AUDIT FINDINGS (Beat Saber 1.44.3)
    // D1. The 1.42.2 Custom/SetDepthOnly Properties block contains exactly the
    //     four controls below. Defaults are Ref=0, Always, Keep, and ZWrite On.
    // D2 [vertex-66aea619521c0b3f]: the no-keyword vertex reads POSITION and
    //     COLOR, saturates COLOR, and performs object-to-world then world-to-clip.
    // D3 [vertex-d5d8e882c98a2cc8]: STEREO_INSTANCING_ON selects the eye matrix
    //     and render-target slice. No general INSTANCING_ON variant was compiled.
    // D4 [fragment-058977666c847ef9,6c57a8d760f7e76d]: both fragments return the
    //     interpolated saturated vertex color without textures or other features.
    // D5. All 35 recovered game materials use Ref=1, Always, Replace, ZWrite Off.
    //     Their explicit material overrides differ intentionally from shader defaults.
    // D6. Serialized original object 4570503278470229751 uses Cull Back,
    //     ColorMask 0, and One/Zero blending. The shader queue supplies the
    //     original material's saved order (1949) for current inheriting materials.
    Properties
    {
        _StencilRefValue ("Stencil Ref Value", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp Func", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencill Pass Op", Float) = 0
        [Toggle] _ZWrite ("Z Write", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-51"
            "RenderType" = "Opaque"
        }
        Pass
        {
            Name ""
            Blend One Zero, One Zero
            ColorMask 0
            ZClip On
            ZWrite [_ZWrite]
            Cull Back

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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 color : TEXCOORD0;
                float4 position : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position = UnityObjectToClipPos(v.vertex);
                o.color = saturate(v.color);
                return o;
            }

            fixed4 frag(v2f inp) : SV_Target
            {
                return inp.color;
            }
            ENDCG
        }
    }
}