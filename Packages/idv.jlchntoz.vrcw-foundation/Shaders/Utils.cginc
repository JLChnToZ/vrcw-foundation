#ifndef VRCW_FOUNDATION_UTILS_INCLUDED
#define VRCW_FOUNDATION_UTILS_INCLUDED

#include "UnityCG.cginc"

inline float median(float3 col) {
    return max(min(col.r, col.g), min(max(col.r, col.g), col.b));
}

inline bool tryNormalize(inout float3 col) {
    float sqrLen = dot(col, col);
    if (sqrLen < 0.0001) return 0;
    col *= rsqrt(sqrLen);
    return 1;
}

inline float4x4 billboard() {
    float4x4 m;
    #ifdef USING_STEREO_MATRICES
        m = (unity_StereoCameraToWorld[0] + unity_StereoCameraToWorld[1]) * 0.5;
    #else
        m = unity_CameraToWorld;
    #endif
    m = mul(m, unity_WorldToObject);
    m._14_24_34_44 = float4(0, 0, 0, 1);
    float3 v0 = m._11_21_31;
    if (!tryNormalize(v0)) return m;
    float3 v1 = m._12_22_32;
    v1 -= dot(v1, v0) * v0;
    if (!tryNormalize(v1)) return m;
    m._11_21_31 = v0;
    m._12_22_32 = v1;
    m._13_23_33 = cross(v0, v1);
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
    #ifdef USING_STEREO_MATRICES
        float4 left = proj2world(localpos, 0);
        float4 right = proj2world(localpos, 1);
        localpos = mul(unity_CameraProjection, mul(unity_WorldToCamera, (left + right) * 0.5));
    #endif
    localpos.z = saturate((localpos.z - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y));
    #if UNITY_REVERSED_Z
        localpos.z = 1 - localpos.z;
    #endif
    return localpos;
}
#endif