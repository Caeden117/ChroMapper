#ifndef CHROMAPPER_REFLECTION_INCLUDED
#define CHROMAPPER_REFLECTION_INCLUDED

#include "../Core/Data.hlsl"
#include "Lighting.hlsl"

// All per-material reflection inputs are passed in as arguments; the library
// does not read uniforms the consumer shader must declare.

inline float3 DecodeReflectionProbeChannel(float channel, float4 lightBakeId)
{
    float low = min(channel, 0.5);
    float high = max(channel - 0.5, 0.0) * lightBakeId.w;
    return low * lightBakeId.rgb + high * high;
}

inline float3 DecodeReflectionProbePair(
    float3 probe1, float3 probe2,
    float4 lightProbeLightBakeIdA, float4 lightProbeLightBakeIdB,
    float4 lightProbeLightBakeIdC, float4 lightProbeLightBakeIdD,
    float4 lightProbeLightBakeIdE, float4 lightProbeLightBakeIdF,
    float reflectionProbeIntensity)
{
    float3 decoded = DecodeReflectionProbeChannel(probe1.r, lightProbeLightBakeIdA);
    decoded += DecodeReflectionProbeChannel(probe1.g, lightProbeLightBakeIdB);
    decoded = DecodeReflectionProbeChannel(probe1.b, lightProbeLightBakeIdC) + decoded;
    decoded += DecodeReflectionProbeChannel(probe2.r, lightProbeLightBakeIdD);
    decoded = DecodeReflectionProbeChannel(probe2.g, lightProbeLightBakeIdE) + decoded;
    decoded = DecodeReflectionProbeChannel(probe2.b, lightProbeLightBakeIdF) + decoded;
    return saturate(decoded * 2.0) * reflectionProbeIntensity;
}

inline float3 SampleReflectionProbePairLod(
    float3 reflectionDirection, float reflectionLod,
    samplerCUBE reflectionProbeTexture1, samplerCUBE reflectionProbeTexture2,
    float4 lightProbeLightBakeIdA, float4 lightProbeLightBakeIdB,
    float4 lightProbeLightBakeIdC, float4 lightProbeLightBakeIdD,
    float4 lightProbeLightBakeIdE, float4 lightProbeLightBakeIdF,
    float reflectionProbeIntensity)
{
    float3 probe1 = texCUBElod(
        reflectionProbeTexture1, float4(reflectionDirection, reflectionLod)).rgb;
    float3 probe2 = texCUBElod(
        reflectionProbeTexture2, float4(reflectionDirection, reflectionLod)).rgb;
    return DecodeReflectionProbePair(
        probe1, probe2,
        lightProbeLightBakeIdA, lightProbeLightBakeIdB,
        lightProbeLightBakeIdC, lightProbeLightBakeIdD,
        lightProbeLightBakeIdE, lightProbeLightBakeIdF,
        reflectionProbeIntensity);
}

inline float3 SampleReflectionProbePair(
    float3 reflectionDirection, float smoothness,
    samplerCUBE reflectionProbeTexture1, samplerCUBE reflectionProbeTexture2,
    float4 lightProbeLightBakeIdA, float4 lightProbeLightBakeIdB,
    float4 lightProbeLightBakeIdC, float4 lightProbeLightBakeIdD,
    float4 lightProbeLightBakeIdE, float4 lightProbeLightBakeIdF,
    float reflectionProbeIntensity)
{
    float roughness = 1.0 - smoothness;
    float reflectionLod = roughness * (1.7 - 0.7 * roughness) * 6.0;
    return SampleReflectionProbePairLod(
        reflectionDirection, reflectionLod,
        reflectionProbeTexture1, reflectionProbeTexture2,
        lightProbeLightBakeIdA, lightProbeLightBakeIdB,
        lightProbeLightBakeIdC, lightProbeLightBakeIdD,
        lightProbeLightBakeIdE, lightProbeLightBakeIdF,
        reflectionProbeIntensity);
}

inline float3 BoxProjectReflectionDirection(
    float3 reflectionDirection, float3 worldPosition,
    float3 boundsMin, float3 boundsMax, float3 probePosition)
{
    float3 intersectionBounds;
    intersectionBounds.x = reflectionDirection.x > 0.0 ? boundsMax.x : boundsMin.x;
    intersectionBounds.y = reflectionDirection.y > 0.0 ? boundsMax.y : boundsMin.y;
    intersectionBounds.z = reflectionDirection.z > 0.0 ? boundsMax.z : boundsMin.z;
    float3 intersectionFactors = (intersectionBounds - worldPosition) / reflectionDirection;
    float intersectionDistance = min(intersectionFactors.x,
                                     min(intersectionFactors.y, intersectionFactors.z));
    return reflectionDirection * intersectionDistance + worldPosition - probePosition;
}

#endif
