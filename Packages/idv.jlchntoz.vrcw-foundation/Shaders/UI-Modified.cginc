// Partially based on MUI and MSDF UI Shader from Merlin, and TextMeshPro Mobile SDF Shader from Unity

#include "UnityCG.cginc"
#include "UnityUI.cginc"

#pragma multi_compile_local_vertex _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local_fragment _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
#pragma shader_feature_local_vertex _ _VRC_SUPPORT
#pragma shader_feature_local_vertex _ _MIRROR_FLIP
#pragma shader_feature_local_vertex _ _SCREENSPACE_OVERLAY _BILLBOARD _DOUBLE_SIDED
#ifdef GEOM_SUPPORT
#pragma shader_feature_local_geometry _ _VRC_SUPPORT
#pragma shader_feature_local_geometry _ _MIRROR_FLIP
#pragma shader_feature_local_geometry _ _SCREENSPACE_OVERLAY _BILLBOARD _DOUBLE_SIDED
#endif

#ifdef _VRC_SUPPORT
#include "VRCMirrorCameraSelector.cginc"
#endif
#include "Utils.cginc"

#ifdef MSDF_OVERRIDE
#define MSDF 2
#endif
#ifndef GEOM_SUPPORT
#undef _DOUBLE_SIDED
#endif
#ifdef _SCREENSPACE_OVERLAY
#undef _DOUBLE_SIDED
#undef _BILLBOARD
#endif
#ifdef _DOUBLE_SIDED
#undef _BILLBOARD
#endif

struct appdata_t {
    float4 vertex : POSITION;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    #if TMPRO_SDF
        float2 texcoord1 : TEXCOORD1;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 vertex : SV_POSITION;
    fixed4 color : COLOR;
    #if TMPRO_SDF
        float4 texcoord : TEXCOORD0;
    #else
        float2 texcoord : TEXCOORD0;
    #endif
    float2 generatedtexcoord : TEXCOORD1;
    centroid float2 centroidtexcoord : TEXCOORD2;
    #if defined(UNITY_UI_CLIP_RECT) || defined(_DOUBLE_SIDED)
        float4 localpos : TEXCOORD3;
    #endif
    UNITY_VERTEX_OUTPUT_STEREO
    #if TMPRO_SDF
        UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
};

sampler2D _MainTex;
float4 _MainTex_ST;
#if TMPRO_SDF
    uniform float4 _FaceColor;
    uniform float _TextureWidth;
    uniform float _GradientScale;
    uniform float _ScaleRatioA;
    uniform float _WeightNormal, _WeightBold;
#else
#if MSDF_OVERRIDE
    sampler2D _MSDFTex;
    float4 _MSDFTex_TexelSize;
#else
    float4 _MainTex_TexelSize;
#endif
    fixed4 _Color;
    fixed4 _TextureSampleAdd;
#endif
#if UNITY_UI_CLIP_RECT
    float4 _ClipRect;
#endif
#if MSDF
    float _PixelRange;
#endif

#ifdef _SCREENSPACE_OVERLAY
    float4 _CanvasRect;
    float _AspectRatioMatch;
#endif

#ifndef GEOM_SUPPORT
    int _Cull;
#endif

v2f vert(appdata_t v, uint vertID : SV_VertexID) {
    v2f OUT;
    UNITY_INITIALIZE_OUTPUT(v2f, OUT);
    UNITY_SETUP_INSTANCE_ID(v);
    #if TMPRO_SDF
        UNITY_TRANSFER_INSTANCE_ID(v, OUT);
    #endif
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    float4 localpos = v.vertex;
    #ifdef _VRC_SUPPORT
        if (!isVisibleInVRC()) return OUT;
        #if (defined(_BILLBOARD) && !defined(_MIRROR_FLIP)) || (!defined(_BILLBOARD) && defined(_MIRROR_FLIP))
            if (isInVRCMirror()) localpos.x = -localpos.x;
        #endif
    #endif

    #ifdef _BILLBOARD
        localpos = mul(billboard(), localpos);
    #endif

    #if defined(UNITY_UI_CLIP_RECT) || defined(_DOUBLE_SIDED)
        OUT.localpos = localpos;
    #endif
    #if _SCREENSPACE_OVERLAY
        localpos.xy = (localpos.xy - _CanvasRect.xy) * 2 / _CanvasRect.zw;
        float2 adjustment = _ScreenParams.xy * _CanvasRect.wz;
        adjustment = float2(adjustment.y / adjustment.x, adjustment.x / adjustment.y);
        localpos.xy *= lerp(float2(adjustment.x, 1), float2(1, adjustment.y), _AspectRatioMatch);
        localpos.y = -localpos.y;
        localpos.z = saturate((localpos.z - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y));
        #if UNITY_REVERSED_Z
            localpos.z = 1 - localpos.z;
        #endif
        OUT.vertex = localpos;
    #else
        OUT.vertex = UnityObjectToClipPos(localpos);
    #endif

    #if TMPRO_SDF
        OUT.texcoord = float4(
            v.texcoord.xy,
            0.5 - 0.125 * lerp(_WeightNormal, _WeightBold, step(v.texcoord1.y, 0)) * _ScaleRatioA,
            1.3333333 * _GradientScale / _TextureWidth
        );
        OUT.color = v.color * _FaceColor;
        OUT.color.rgb *= OUT.color.a;
    #else
        OUT.texcoord = v.texcoord;
        OUT.color = v.color * _Color;
    #endif

    OUT.centroidtexcoord = v.texcoord;

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
#ifdef _DOUBLE_SIDED
void appendFlippedV2f(v2f v, inout TriangleStream<v2f> triStream) {
    v.localpos.x = -v.localpos.x;
    v.vertex = UnityObjectToClipPos(v.localpos);
    triStream.Append(v);
}
#endif

#ifdef _DOUBLE_SIDED
[maxvertexcount(6)]
#else
[maxvertexcount(3)]
#endif
void geom(triangle v2f input[3], inout TriangleStream<v2f> triStream) {
    v2f v0, v1, v2;
    #if defined(_VRC_SUPPORT) && defined(_MIRROR_FLIP)
    if (isInVRCMirror()) {
        v0 = input[2];
        v1 = input[1];
        v2 = input[0];
    } else
    #endif
    {
        v0 = input[0];
        v1 = input[1];
        v2 = input[2];
    }
    triStream.Append(v0);
    triStream.Append(v1);
    triStream.Append(v2);
    triStream.RestartStrip();
    #ifdef _DOUBLE_SIDED
        appendFlippedV2f(v0, triStream);
        appendFlippedV2f(v1, triStream);
        appendFlippedV2f(v2, triStream);
        triStream.RestartStrip();
    #endif
}
#endif

half4 frag(
    v2f IN
#ifndef GEOM_SUPPORT
    , fixed facing : VFACE
#endif
) : SV_Target {
    #if TMPRO_SDF
        UNITY_SETUP_INSTANCE_ID(IN);
    #endif
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

    #if TMPRO_SDF
        float d = tex2D(_MainTex, texcoord).a;
        float2 ddxuv = ddx(texcoord);
        float2 ddyuv = ddy(texcoord).yx;
        ddyuv.x = -ddyuv.x;
        color *= saturate((d - IN.texcoord.z) * rsqrt(abs(dot(ddxuv, ddyuv))) * IN.texcoord.w + 0.5);
    #elif MSDF
    #ifdef MSDF_OVERRIDE
        float2 msdfUnit = _PixelRange * _MSDFTex_TexelSize.xy;
        float4 sampleCol = tex2D(_MSDFTex, texcoord);
    #else
        float2 msdfUnit = _PixelRange * _MainTex_TexelSize.xy;
        float4 sampleCol = tex2D(_MainTex, texcoord);
    #endif
        float sigDist = median(sampleCol.xyz) - 0.5;
        sigDist *= max(dot(msdfUnit, 0.5 / fwidth(texcoord)), 1); // Max to handle fading out to quads in the distance
        float opacity = saturate(sigDist + 0.5);
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
