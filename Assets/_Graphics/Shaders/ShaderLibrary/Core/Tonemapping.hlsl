// ETAN shared this ACES fit.
// Tonemapping: https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
// This shared fit changes RGB only; alpha is unchanged. Reinhard is owned by Bloom.shader.
#ifndef CHROMAPPER_TONEMAPPING_INCLUDED
#define CHROMAPPER_TONEMAPPING_INCLUDED

inline float4 ApplyAcesTonemapping(float4 col)
{
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    col.rgb = saturate(col.rgb * (a * col.rgb + b) / (col.rgb * (c * col.rgb + d) + e));
    return col;
}

#endif // CHROMAPPER_TONEMAPPING_INCLUDED
