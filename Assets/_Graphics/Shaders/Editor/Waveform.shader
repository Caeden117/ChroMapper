Shader "ChroMapper/Editor/Waveform"
{
    Properties
    {
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float4 local : TEXCOORD1; };
            StructuredBuffer<float2> _Peaks;
            uint _PeakCount;
            uint _Ready;
            float _SamplesPerPeak;
            float _SampleCount;
            float _ViewStart;
            float _ViewEnd;
            float _Rectify;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.local = v.vertex;
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                if (_Ready == 0) return 0;
                float sample = lerp(_ViewStart, _ViewEnd, i.uv.x);
                if (sample < 0 || sample >= _SampleCount) return 0;
                uint index = min((uint)(sample / _SamplesPerPeak), _PeakCount - 1);
                float2 peak = clamp(_Peaks[index], -1, 1);
                float lower = _Rectify > 0.5 ? 0 : 0.5 + peak.x * 0.47;
                float upper = _Rectify > 0.5 ? max(abs(peak.x), abs(peak.y)) * 0.94 : 0.5 + peak.y * 0.47;
                float pixel = max(fwidth(i.uv.y), 0.00001);
                upper = max(upper, lower + pixel * 0.5);
                i.color.a *= saturate((i.uv.y - lower) / pixel + 0.5) * saturate((upper - i.uv.y) / pixel + 0.5);
                #ifdef UNITY_UI_CLIP_RECT
                    i.color.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                return i.color;
            }
            ENDHLSL
        }
    }
}