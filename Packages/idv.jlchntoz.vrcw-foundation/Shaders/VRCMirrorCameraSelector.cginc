/*
Declare this property in shader lab:
    [EnumMask(Direct Look, VR Handheld Camera, Desktop Handheld Camera, Screenshot, VR Mirror, VR Handheld Camera in Mirror, _, VR Screenshot in Mirror, Desktop Mirror, _, Desktop Handheld Camera in Mirror, Desktop Screenshot in Mirror)]
    _RenderMode ("Visible Modes", Int) = 4095
*/
#ifndef VRC_MIRROR_CAMERA_SELECTOR_INCLUDED
#define VRC_MIRROR_CAMERA_SELECTOR_INCLUDED
int _RenderMode;

int _VRChatCameraMode; // 0 = Normal, 1 = VR Handheld Camera, 2 = Desktop Handheld Camera, 3 = Screenshot
int _VRChatMirrorMode; // 0 = Normal, 1 = VR Mirror, 2 = Desktop Mirror

inline bool isVisibleInVRC() {
    uint currentRenderMode = 1 << (uint)(_VRChatCameraMode + _VRChatMirrorMode * 4);
    return (_RenderMode & currentRenderMode) != 0;
}

inline bool isInVRCMirror() {
    return _VRChatMirrorMode > 0;
}
#endif