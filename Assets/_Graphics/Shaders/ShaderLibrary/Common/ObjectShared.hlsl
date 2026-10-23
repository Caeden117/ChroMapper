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

inline void ApplyObjectEditorDither(float alpha, float4 screenPosition)
{
    if (alpha < 1.0)
    {
        const float thresholds[16] =
        {
            1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
            13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
            4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
            16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
        };
        int2 pixel = int2((screenPosition.xy / screenPosition.w) * _ScreenParams.xy);
        int index = (pixel.x & 3) * 4 + (pixel.y & 3);
        clip(alpha - thresholds[index]);
    }
}

#endif
