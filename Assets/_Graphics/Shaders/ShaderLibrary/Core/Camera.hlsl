#ifndef CHROMAPPER_CAMERA_INCLUDED
#define CHROMAPPER_CAMERA_INCLUDED

float2 _StereoCameraEyeOffsets;

inline float3 GetStereoAwareCameraPosition()
{
    #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
    #else
    return _WorldSpaceCameraPos;
    #endif
}

inline float4 ComputeScreenPosCustom(float4 pos)
{
    float4 screenPos = ComputeNonStereoScreenPos(pos);
    #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    screenPos.x += pos.w * _StereoCameraEyeOffsets[unity_StereoEyeIndex];
    #if !UNITY_UV_STARTS_AT_TOP
    screenPos.y = -screenPos.y + pos.w;
    #endif
    #endif
    return screenPos;
}

#endif
