Shader "UI/Modified" {
    Properties {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        [Header(MSDF Settings)]
        [Toggle(MSDF)] _UseMSDF ("Input is MSDF", Float) = 0
        [Toggle(MSDF_OVERRIDE)] _OverrideMSDF ("Use Override Texture", Float) = 0
        [NoScaleOffset] _MSDFTex ("Override Texture", 2D) = "black" {}
		_PixelRange ("MSDF Pixel Range", Float) = 4.0

        [Header(Conditional Appearance in VRChat Camera Mirror)]
        [Toggle(_VRC_SUPPORT)] _VRCSupport ("Enable", Int) = 0
        [EnumMask(Direct Look, VR Handheld Camera, Desktop Handheld Camera, Screenshot, VR Mirror, VR Handheld Camera in Mirror, _, VR Screenshot in Mirror, Desktop Mirror, _, Desktop Handheld Camera in Mirror, Desktop Screenshot in Mirror)]
        _RenderMode ("Visible Modes", Int) = 4095
        [Toggle(_MIRROR_FLIP)] _MirrorFlip ("Flip in Mirror", Int) = 0

        [Header(Experimental Settings)]
        [Toggle(_BILLBOARD)] _Billboard ("Billboard (Require Zero Rotation On Transform)", Int) = 0
        [Toggle(_DOUBLE_SIDED)] _DoubleSided ("Double Sided (PC Only)", Int) = 0

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
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Int) = 2
        [EnumMask(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("Color Mask", Int) = 15
        [Toggle(_)] _ZWrite ("Z Write", Int) = 0
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

        LOD 300
        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
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
            #pragma shader_feature_local __ MSDF MSDF_OVERRIDE
            #define GEOM_SUPPORT
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass {
            Name "Fallback"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma shader_feature_local __ MSDF MSDF_OVERRIDE
            #include "./UI-Modified.cginc"
            ENDCG
        }
    }

    Fallback "UI/Default"
}
