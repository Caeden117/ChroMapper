#ifndef CHROMAPPER_LIGHTING_INCLUDED
#define CHROMAPPER_LIGHTING_INCLUDED

#include "../Core/Data.hlsl"
#include "../Core/Camera.hlsl"

uniform float4 _DirectionalLightPositions[5];
uniform float _DirectionalLightRadii[5];
uniform float4 _DirectionalLightDirections[5];
uniform float4 _DirectionalLightColors[5];
uniform float4 _PrivatePointLightPosition;
uniform float _PrivatePointLightIntensity;

inline float CalculateLightFalloff(float3 worldPosition, int lightIndex)
{
    float3 lightOffset = worldPosition - _DirectionalLightPositions[lightIndex].xyz;
    float radiusSquared = _DirectionalLightRadii[lightIndex] * _DirectionalLightRadii[lightIndex];
    return 1.0 / (dot(lightOffset, lightOffset) / radiusSquared * 25.0 + 1.0);
}

inline float CalculateDirectionalDiffuseTerm(float normalDot,
                                             float bothSidesDiffuseMultiplier = 1.0)
{
    #if defined(BOTH_SIDES_DIFFUSE)
    return max(normalDot, 0.0) + min(normalDot, 0.0) * (-bothSidesDiffuseMultiplier);
    #else
    return max(normalDot, 0.0);
    #endif
}

inline float3 CalculateLightDiffuseAccumulation(
    float3 normalWS, float bothSidesDiffuseMultiplier)
{
    float3 direct = CalculateDirectionalDiffuseTerm(
        dot(normalWS, _DirectionalLightDirections[1].xyz),
        bothSidesDiffuseMultiplier) * _DirectionalLightColors[1].rgb;
    direct += CalculateDirectionalDiffuseTerm(
        dot(normalWS, _DirectionalLightDirections[0].xyz),
        bothSidesDiffuseMultiplier) * _DirectionalLightColors[0].rgb;
    direct += CalculateDirectionalDiffuseTerm(
        dot(normalWS, _DirectionalLightDirections[2].xyz),
        bothSidesDiffuseMultiplier) * _DirectionalLightColors[2].rgb;
    direct += CalculateDirectionalDiffuseTerm(
        dot(normalWS, _DirectionalLightDirections[3].xyz),
        bothSidesDiffuseMultiplier) * _DirectionalLightColors[3].rgb;
    direct += CalculateDirectionalDiffuseTerm(
        dot(normalWS, _DirectionalLightDirections[4].xyz),
        bothSidesDiffuseMultiplier) * _DirectionalLightColors[4].rgb;
    return direct;
}

inline float3 CalculateLightDiffuse(float3 normalWS,
                                    float bothSidesDiffuseMultiplier = 1.0)
{
    return CalculateLightDiffuseAccumulation(normalWS, bothSidesDiffuseMultiplier);
}

inline float3 CalculateViewReflectionDirection(float3 worldPosition, float3 normalWS)
{
    float3 cameraPosition = GetStereoAwareCameraPosition();
    float3 viewDirection = normalize(worldPosition - cameraPosition);
    return viewDirection - 2.0 * dot(viewDirection, normalWS) * normalWS;
}

inline float CalculateSpecularLobeFactor(
    float3 lightDirection, float3 reflectionDirection, float specularScale)
{
    float3 difference = lightDirection - reflectionDirection;
    float lobe = saturate(1.0 - dot(difference, difference) * specularScale * 0.5);
    lobe *= lobe;
    lobe *= lobe;
    lobe *= lobe;
    return lobe;
}

inline float3 CalculateLightSpecularLobe(float3 lightDirection, float3 lightColor,
                                         float3 reflectionDirection, float specularScale)
{
    float lobe = CalculateSpecularLobeFactor(
        lightDirection, reflectionDirection, specularScale);
    return lobe * lightColor * specularScale;
}

inline float3 CalculateSpecularReflectionDirection(
    float3 worldPosition, float3 normalWS, float smoothness, out float specularScale)
{
    float3 reflectionDirection = CalculateViewReflectionDirection(worldPosition, normalWS);
    float smoothnessSquared = smoothness * smoothness;
    specularScale = smoothnessSquared * smoothnessSquared * 500.0;
    return reflectionDirection;
}

inline float3 CalculateLightSpecular(float3 worldPosition, float3 normalWS, float smoothness)
{
    float specularScale;
    float3 reflectionDirection = CalculateSpecularReflectionDirection(
        worldPosition, normalWS, smoothness, specularScale);

    float3 specular = CalculateLightSpecularLobe(
        _DirectionalLightDirections[1].xyz, _DirectionalLightColors[1].rgb,
        reflectionDirection, specularScale);
    specular += CalculateLightSpecularLobe(
        _DirectionalLightDirections[0].xyz, _DirectionalLightColors[0].rgb,
        reflectionDirection, specularScale);
    specular += CalculateLightSpecularLobe(
        _DirectionalLightDirections[2].xyz, _DirectionalLightColors[2].rgb,
        reflectionDirection, specularScale);
    specular += CalculateLightSpecularLobe(
        _DirectionalLightDirections[3].xyz, _DirectionalLightColors[3].rgb,
        reflectionDirection, specularScale);
    specular += CalculateLightSpecularLobe(
        _DirectionalLightDirections[4].xyz, _DirectionalLightColors[4].rgb,
        reflectionDirection, specularScale);
    return specular;
}

inline float3 CalculateLightFalloffDiffuse(float3 worldPosition, float3 normalWS)
{
    float3 direct = max(dot(normalWS, _DirectionalLightDirections[1].xyz), 0.0) *
        _DirectionalLightColors[1].rgb * CalculateLightFalloff(worldPosition, 1);
    direct += max(dot(normalWS, _DirectionalLightDirections[0].xyz), 0.0) *
        _DirectionalLightColors[0].rgb * CalculateLightFalloff(worldPosition, 0);
    direct += max(dot(normalWS, _DirectionalLightDirections[2].xyz), 0.0) *
        _DirectionalLightColors[2].rgb * CalculateLightFalloff(worldPosition, 2);
    direct += max(dot(normalWS, _DirectionalLightDirections[3].xyz), 0.0) *
        _DirectionalLightColors[3].rgb * CalculateLightFalloff(worldPosition, 3);
    direct += max(dot(normalWS, _DirectionalLightDirections[4].xyz), 0.0) *
        _DirectionalLightColors[4].rgb * CalculateLightFalloff(worldPosition, 4);
    return direct;
}

inline float3 CalculateLightFalloffSpecularLobe(float3 lightDirection, float3 lightColor,
                                                float3 reflectionDirection,
                                                float specularScale, float falloff)
{
    float lobe = CalculateSpecularLobeFactor(
        lightDirection, reflectionDirection, specularScale);
    return lobe * lightColor * falloff * specularScale;
}

inline float3 CalculateLightFalloffSpecular(float3 worldPosition, float3 normalWS,
                                            float smoothness)
{
    float specularScale;
    float3 reflectionDirection = CalculateSpecularReflectionDirection(
        worldPosition, normalWS, smoothness, specularScale);

    float3 specular = CalculateLightFalloffSpecularLobe(
        _DirectionalLightDirections[1].xyz, _DirectionalLightColors[1].rgb,
        reflectionDirection, specularScale,
        CalculateLightFalloff(worldPosition, 1));
    specular += CalculateLightFalloffSpecularLobe(
        _DirectionalLightDirections[0].xyz, _DirectionalLightColors[0].rgb,
        reflectionDirection, specularScale,
        CalculateLightFalloff(worldPosition, 0));
    specular += CalculateLightFalloffSpecularLobe(
        _DirectionalLightDirections[2].xyz, _DirectionalLightColors[2].rgb,
        reflectionDirection, specularScale,
        CalculateLightFalloff(worldPosition, 2));
    specular += CalculateLightFalloffSpecularLobe(
        _DirectionalLightDirections[3].xyz, _DirectionalLightColors[3].rgb,
        reflectionDirection, specularScale,
        CalculateLightFalloff(worldPosition, 3));
    specular += CalculateLightFalloffSpecularLobe(
        _DirectionalLightDirections[4].xyz, _DirectionalLightColors[4].rgb,
        reflectionDirection, specularScale,
        CalculateLightFalloff(worldPosition, 4));
    return specular;
}

#endif
