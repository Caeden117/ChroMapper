#ifndef CHROMAPPER_OVERDRAW_INCLUDED
#define CHROMAPPER_OVERDRAW_INCLUDED

float _TrueOverdrawOn;
float _OpaqueOverdrawOn;
float _TransparentOverdrawOn;
float4 _OverdrawColor;

inline float4 CalculateOpaqueOverdrawColor()
{
    precise float overdraw = _TrueOverdrawOn * _OpaqueOverdrawOn;
    overdraw *= asfloat(0x3dcccccdu);
    precise float4 overdrawColor = overdraw * _OverdrawColor;
    return overdrawColor;
}

// Both native parametric light shaders use the transparent gate, including OpaqueNeonLight.
inline float4 CalculateParametricOverdrawColor()
{
    precise float overdraw = _TransparentOverdrawOn * asfloat(0x3dcccccdu);
    precise float4 overdrawColor = overdraw * _OverdrawColor;
    return overdrawColor;
}

#endif
