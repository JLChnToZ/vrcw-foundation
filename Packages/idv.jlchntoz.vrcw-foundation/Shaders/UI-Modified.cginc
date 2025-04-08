// Based on Merlin's MUI and MSDF UI Shader

#include "UnityCG.cginc"
#include "UnityUI.cginc"

#pragma multi_compile_local __ MSDF
#pragma multi_compile_local __ MSDF_OVERRIDE
#pragma multi_compile_local __ UNITY_UI_CLIP_RECT
#pragma multi_compile_local __ UNITY_UI_ALPHACLIP
#pragma shader_feature_local __ _VRC_SUPPORT
#pragma shader_feature_local __ _MIRROR_FLIP
#pragma shader_feature_local __ _BILLBOARD

struct appdata_t {
    float4 vertex : POSITION;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 vertex : SV_POSITION;
    fixed4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    float2 generatedtexcoord : TEXCOORD1;
    centroid float2 centroidtexcoord : TEXCOORD2;
    #ifdef UNITY_UI_CLIP_RECT
        float4 localpos : TEXCOORD3;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _MainTex;
float4 _MainTex_ST;
#if MSDF_OVERRIDE
    sampler2D _MSDFTex;
    float4 _MSDFTex_TexelSize;
#else
    float4 _MainTex_TexelSize;
#endif
fixed4 _Color;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float _PixelRange;

#ifdef _VRC_SUPPORT
    int _RenderMode;

    int _VRChatCameraMode; // 0 = Normal, 1 = VR Handheld Camera, 2 = Desktop Handheld Camera, 3 = Screenshot
    int _VRChatMirrorMode; // 0 = Normal, 1 = VR Mirror, 2 = Desktop Mirror
#endif

#ifndef GEOM_SUPPORT
    int _Cull;
#endif

float median(float3 col) {
    return max(min(col.r, col.g), min(max(col.r, col.g), col.b));
}

bool tryNormalize(inout float3 col) {
    float sqrLen = dot(col, col);
    if (sqrLen < 0.0001f) return 0;
    col *= rsqrt(sqrLen);
    return 1;
}

float4x4 billboard() {
    float4x4 m = mul(unity_CameraToWorld, unity_WorldToObject);
    m._m03_m13_m23_m33 = float4(0, 0, 0, 1);
    float3 v0 = m._m00_m10_m20;
    if (!tryNormalize(v0)) return m;
    float3 v1 = m._m01_m11_m21;
    v1 -= dot(v1, v0) * v0;
    if (!tryNormalize(v1)) return m;
    m._m00_m10_m20 = v0;
    m._m01_m11_m21 = v1;
    m._m02_m12_m22 = cross(v0, v1);
    return m;
}

v2f vert(appdata_t v, uint vertID : SV_VertexID) {
    v2f OUT;
    UNITY_INITIALIZE_OUTPUT(v2f, OUT);
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    float4 localpos = v.vertex;
    #ifdef _VRC_SUPPORT
        uint currentRenderMode = pow(2, _VRChatCameraMode + _VRChatMirrorMode * 4);
        if (_RenderMode / currentRenderMode % 2 == 0) return OUT;
        #if (defined(_BILLBOARD) && !defined(_MIRROR_FLIP)) || (!defined(_BILLBOARD) && defined(_MIRROR_FLIP))
            if (_VRChatMirrorMode > 0) localpos.x = -localpos.x;
        #endif
    #endif

    #ifdef _BILLBOARD
        localpos = mul(billboard(), localpos);
    #endif

    #ifdef UNITY_UI_CLIP_RECT
        OUT.localpos = localpos;
    #endif
    OUT.vertex = UnityObjectToClipPos(localpos);

    OUT.texcoord = v.texcoord;
    OUT.centroidtexcoord = v.texcoord;

    OUT.color = v.color * _Color;

    const float2 generatedCoords[4] = {
        float2(0, 0),
        float2(1, 0),
        float2(1, 1),
        float2(0, 1),
    };

    OUT.generatedtexcoord = generatedCoords[vertID % 4];
    return OUT;
}

#ifdef GEOM_SUPPORT
[maxvertexcount(3)]
void geom(triangle v2f input[3], inout TriangleStream<v2f> triStream) {
    #if defined(_VRC_SUPPORT) && defined(_MIRROR_FLIP)
    if (_VRChatMirrorMode > 0) {
        triStream.Append(input[0]);
        triStream.Append(input[2]);
        triStream.Append(input[1]);
    } else
    #endif
    {
        triStream.Append(input[0]);
        triStream.Append(input[1]);
        triStream.Append(input[2]);
    }
    triStream.RestartStrip();
}
#endif

half4 frag(
    v2f IN
#ifndef GEOM_SUPPORT
    , fixed facing : VFACE
#endif
) : SV_Target {
    #ifndef GEOM_SUPPORT
        switch (_Cull) {
            case 1: if (facing > 0) discard; break;
            case 2: if (facing < 0) discard; break;
        }
    #endif
    float4 color = IN.color;
    float2 texcoord = IN.texcoord;

    // Use Valve method of falling back to centroid interpolation if you fall out of valid interpolation range
    // Based on slide 44 of http://media.steampowered.com/apps/valve/2015/Alex_Vlachos_Advanced_VR_Rendering_GDC2015.pdf
    if (any(IN.generatedtexcoord > 1) || any(IN.generatedtexcoord < 0))
        texcoord = IN.centroidtexcoord;

    #ifdef MSDF
    #ifdef MSDF_OVERRIDE
        float2 msdfUnit = _PixelRange / _MSDFTex_TexelSize.zw;
        float4 sampleCol = tex2D(_MSDFTex, texcoord);
    #else
        float2 msdfUnit = _PixelRange / _MainTex_TexelSize.zw;
        float4 sampleCol = tex2D(_MainTex, texcoord);
    #endif
        float sigDist = median(sampleCol.xyz) - 0.5;
        sigDist *= max(dot(msdfUnit, 0.5 / fwidth(texcoord)), 1); // Max to handle fading out to quads in the distance
        float opacity = clamp(sigDist + 0.5, 0.0, 1.0);
        color *= float4(1, 1, 1, opacity) + _TextureSampleAdd;
    #else
        color *= tex2D(_MainTex, texcoord) + _TextureSampleAdd;
    #endif

    #ifdef UNITY_UI_CLIP_RECT
        color.a *= UnityGet2DClipping(IN.localpos.xy, _ClipRect);
    #endif

    #ifdef UNITY_UI_ALPHACLIP
        clip(color.a - 0.001);
    #endif

    return color;
}
