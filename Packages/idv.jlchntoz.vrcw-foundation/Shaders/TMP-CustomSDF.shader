Shader "TextMeshPro/Custom SDF" {
    Properties {
        [HDR] [MainColor] _FaceColor ("Face Color", Color) = (1, 1, 1, 1)

        [Header(Properties Controlled by TextMeshPro)]
        [NoScaleOffset] _MainTex ("Font Atlas", 2D) = "black" {}
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _GradientScale ("Gradient Scale", Float) = 5.0
        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _ScaleRatioA ("Scale Ratio A", Float) = 1

        [Header(Conditional Appearance in VRChat Camera Mirror)]
        [Toggle(_VRC_SUPPORT)] _VRCSupport ("Enable", Int) = 0
        [EnumMask(Direct Look, VR Handheld Camera, Desktop Handheld Camera, Screenshot, VR Mirror, VR Handheld Camera in Mirror, _, VR Screenshot in Mirror, Desktop Mirror, _, Desktop Handheld Camera in Mirror, Desktop Screenshot in Mirror)]
        _RenderMode ("Visible Modes", Int) = 4095
        [Toggle(_MIRROR_FLIP)] _MirrorFlip ("Flip in Mirror", Int) = 0

        [Header(Experimental Settings)]
        [Toggle(_DOUBLE_SIDED)] _DoubleSided ("Double Sided (PC Only)", Int) = 0
        [Toggle(_BILLBOARD)] _Billboard ("Billboard (Require Zero Rotation On Transform)", Int) = 0
        [Toggle(_SCREENSPACE_OVERLAY)] _ScreenSpaceOverlay ("Screen Space Overlay", Int) = 0

        [Header(Screenspace Overlay Settings)]
        _CanvasRect ("Canvas Rect", Vector) = (0, 0, 1, 1)
        _AspectRatioMatch ("Aspect Ratio Match (0 = Width, 1 = Height)", Range(0, 1)) = 0.5

        [Header(Render Pipeline Settings)]
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Space]
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Comparison Mode", Int) = 8
        [IntRange] _Stencil ("ID", Range(0, 255)) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Operation", Int) = 0
        [IntRange] _StencilWriteMask ("Write Mask", Range(0, 255)) = 255
        [IntRange] _StencilReadMask ("Read Mask", Range(0, 255)) = 255
        [Space]
        [Enum(UnityEngine.Rendering.CompareFunction)] unity_GUIZTestMode("Z Test Mode", Int) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Int) = 2
        [EnumMask(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("Color Mask", Int) = 15
        [Toggle(_)] _ZWrite ("Z Write", Int) = 0
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        LOD 300
        Cull [_CullMode]
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [unity_GUIZTestMode]
        Fog { Mode Off }
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            Name "Full"
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            // Exclude renderers incompatible with the geometry stage when writing to screen
            #pragma exclude_renderers gles gles3 glcore metal
            #define GEOM_SUPPORT
            #define TMPRO_SDF 1
            #include "./UI-Modified.cginc"
            ENDCG
        }
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        LOD 200
        Cull Off
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [unity_GUIZTestMode]
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            Name "Fallback"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #define TMPRO_SDF 1
            #include "./UI-Modified.cginc"
            ENDCG
        }
    }

    Fallback "UI/Default"
}
