#ifndef VRCW_FOUNDATION_UTILS_INCLUDED
#define VRCW_FOUNDATION_UTILS_INCLUDED

#include "UnityCG.cginc"

inline float median(float3 col) {
    return max(min(col.r, col.g), min(max(col.r, col.g), col.b));
}

inline bool tryNormalize(inout float3 col) {
    float sqrLen = dot(col, col);
    if (sqrLen < 0.0001220703125) return 0;
    col *= rsqrt(sqrLen);
    return 1;
}

inline bool tryCalculateUp(inout float3 up, float3 forward) {
    if (!tryNormalize(up)) return 0;
    up -= dot(up, forward) * forward;
    return tryNormalize(up);
}

#ifdef USING_STEREO_MATRICES
#define _CenterCameraToWorld (unity_StereoCameraToWorld[0] + unity_StereoCameraToWorld[1]) * 0.5
#else
#define _CenterCameraToWorld unity_CameraToWorld
#endif

inline float4x4 billboard() {
    float4x4 cam2World = _CenterCameraToWorld;
    float4x4 world2Obj = unity_WorldToObject;
    float4x4 m = mul(cam2World, world2Obj);
    m._14_24_34_44 = float4(0, 0, 0, 1);
    float3 z = m._13_23_33;
    if (!tryNormalize(z)) return m;
    float3 y = world2Obj._12_22_32;
    if (!tryCalculateUp(y, z)) {
        y = m._12_22_32;
        if (!tryCalculateUp(y, z)) return m;
    }
    float3 x = cross(y, z);
    m._11_21_31 = x;
    m._12_22_32 = y;
    m._13_23_33 = cross(x, y);
    return m;
}

inline void noclip(inout float4 clipPos) {
    #ifdef SHADER_TARGET_GLSL
        clipPos.z = clamp(clipPos.z, -0.999999, 0.999999);
    #else
        clipPos.z = clamp(clipPos.z, 0.000001, 0.999999);
    #endif
}

inline void nearest(inout float4 clipPos) {
    #ifdef SHADER_TARGET_GLSL
        clipPos.zw = float2(-0.999999, 1.0);
    #else
        clipPos.zw = float2(0.999999, 1.0);
    #endif
}

inline void farthest(inout float4 clipPos) {
    #ifdef SHADER_TARGET_GLSL
        clipPos.zw = float2(0.999999, 1.0);
    #else
        clipPos.zw = float2(0.000001, 1.0);
    #endif
}

#ifdef USING_STEREO_MATRICES
inline float4 proj2world(float4 clipPos, int eye) {
    return mul(unity_StereoCameraToWorld[eye], mul(unity_StereoCameraInvProjection[eye], clipPos));
}
#endif

inline float4 directScreenSpace(float4 localpos) {
    localpos.y = -localpos.y;
    localpos.z = saturate((localpos.z - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y));
    #if UNITY_REVERSED_Z
        localpos.z = 1 - localpos.z;
    #endif
    #ifdef USING_STEREO_MATRICES
        float4 left = proj2world(localpos, 0);
        float4 right = proj2world(localpos, 1);
        localpos = mul(unity_CameraProjection, mul(unity_WorldToCamera, (left + right) * 0.5));
    #endif
    return localpos;
}
#endif