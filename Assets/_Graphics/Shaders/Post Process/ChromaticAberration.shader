// ChroMapper chromatic aberration (editor cameras).
//
// Split out of ChroMapper/Post Process/Bloom so each effect owns its own
// shader instead of sharing pass 6 of the bloom shader. This is the PPv2 Uber
// CHROMATIC_ABERRATION pass: spectral samples along the radial offset with the
// default R/G/B spectral lookup, running as pass 0 (the only pass). The
// scene-owned ChromaticAberrationRenderer runs after the main-effect compositor.
Shader "ChroMapper/Post Process/Chromatic Aberration"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        [HideInInspector] _BloomTexelSize ("Source Texel Size", Vector) = (1, 1, 1, 1)
        // PPv2 Uber _ChromaticAberration_Amount = intensity * 0.05, set per
        // frame by ChromaticAberrationRenderer from its serialized intensity
        // (default 0.1, the value of the mapper scene's PPv2 profile).
        _ChromaticAberration ("Chromatic Aberration Amount", Float) = 0
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"

    // Unity 6 no longer auto-injects the built-in RP texture macros into
    // HLSLINCLUDE/HLSLPROGRAM blocks; define the subset this shader uses so
    // it compiles standalone (same fix as Bloom.shader).
    #ifndef TEXTURE2D_ARGS
    #define TEXTURE2D_ARGS(textureName, samplerName) Texture2D textureName, SamplerState samplerName
    #define TEXTURE2D_PARAM(textureName, samplerName) textureName, samplerName
    #define TEXTURE2D_SAMPLER2D(textureName, samplerName) Texture2D textureName; SamplerState samplerName
    #define SAMPLE_TEXTURE2D(textureName, samplerName, coord) textureName.Sample(samplerName, coord)
    #endif

    struct VaryingsDefault
    {
        float4 vertex : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        float2 texcoordStereo : TEXCOORD1;
    };

    VaryingsDefault VertDefault(appdata_img v)
    {
        VaryingsDefault o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = v.texcoord;
        o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);
        return o;
    }

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

    // Per-blit texel size (xy = 1/size, zw = size), set by ChromaticAberrationRenderer;
    // the zw half drives the distance-based sample count, like the game.
    // (CommandBuffer.Blit does not update _MainTex_TexelSize for temporary targets.)
    float4 _BloomTexelSize;
    float _ChromaticAberration;

    // Bilinear sample of the PPv2 default 3x1 spectral LUT (R, G, B texels,
    // TextureWrapMode.Clamp) at uv.x = t, matching the hardware filtering of
    // the texture the PPv2 stack built.
    float3 SpectralLut(float t)
    {
        float x = t * 3.0 - 0.5;
        float3 r = float3(1.0, 0.0, 0.0);
        float3 g = float3(0.0, 1.0, 0.0);
        float3 b = float3(0.0, 0.0, 1.0);
        if (x <= 0.0) return r;
        if (x < 1.0) return lerp(r, g, x);
        if (x < 2.0) return lerp(g, b, x - 1.0);
        return b;
    }

    float4 FragChromaticAberration(VaryingsDefault i) : SV_Target
    {
        // PPv2 Uber pass (CHROMATIC_ABERRATION): sample the source along the
        // radial path from uv to the "end" point, weighting each sample by the
        // spectral lookup. _ChromaticAberration is intensity * 0.05, set by
        // ChromaticAberrationRenderer; the sample count is distance-driven,
        // clamped to [3, 16].
        float2 uv = i.texcoord;
        float2 coords = 2.0 * uv - 1.0;
        float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration;

        float2 diff = end - uv;
        int samples = clamp(int(length(_BloomTexelSize.zw * diff / 2.0)), 3, 16);
        float2 delta = diff / samples;
        float2 pos = uv;
        float4 sum = 0.0;
        float4 filterSum = 0.0;

        for (int s = 0; s < samples; s++)
        {
            float t = (s + 0.5) / samples;
            float4 texel = _MainTex.SampleLevel(
                sampler_MainTex, UnityStereoTransformScreenSpaceTex(pos), 0.0);
            float4 filter = float4(SpectralLut(t), 1.0);
            sum += texel * filter;
            filterSum += filter;
            pos += delta;
        }

        return sum / filterSum;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 chromatic aberration (the only pass)
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertDefault
            #pragma fragment FragChromaticAberration
            ENDHLSL
        }
    }
}