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
    float4x4 m = mul(unity_CameraToWorld, unity_WorldToObject);
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
#endif