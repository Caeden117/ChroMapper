#ifndef CHROMAPPER_OBJECT_SHARED_INCLUDED
#define CHROMAPPER_OBJECT_SHARED_INCLUDED

inline float3 RotateObjectPositionY(float3 position, float angleRadians)
{
    float sine;
    float cosine;
    sincos(angleRadians, sine, cosine);
    return float3(position.x * cosine - position.z * sine,
                  position.y,
                  position.z * cosine + position.x * sine);
}

inline float4 CalculateRotatedObjectPosition(
    float3 worldPosition, float3 offset, float angleRadians,
    float objectTime, float songTime)
{
    return float4(
        RotateObjectPositionY(worldPosition - offset, angleRadians) + offset,
        objectTime + 0.001 - songTime);
}

#endif
