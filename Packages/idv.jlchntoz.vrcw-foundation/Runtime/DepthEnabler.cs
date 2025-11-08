using UnityEngine;
#if COMPILER_UDONSHARP
using VRC.SDKBase;
using VRC.SDK3.Rendering;
#endif

namespace JLChnToZ.VRC.Foundation {
#if UDONSHARP
    using UdonSharp;

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public partial class DepthEnabler : UdonSharpBehaviour { }
#else
    public partial class DepthEnabler : MonoBehaviour { }
#endif

    [ExecuteInEditMode]
    public partial class DepthEnabler {
        [SerializeField] DepthTextureMode depthMode = DepthTextureMode.Depth;

        void OnEnable() {
#if COMPILER_UDONSHARP
            SetDepthMode(VRCCameraSettings.ScreenCamera);
            SetDepthMode(VRCCameraSettings.PhotoCamera);
#else
            SetDepthMode(Camera.main);
#endif
        }

#if COMPILER_UDONSHARP
        void SetDepthMode(VRCCameraSettings cameraSettings) {
            if (!Utilities.IsValid(cameraSettings)) return;
            // U# don't support bitwise OR operator on API defined enums.
            cameraSettings.DepthTextureMode = (DepthTextureMode)((int)cameraSettings.DepthTextureMode | (int)depthMode);
        }
#else
        void SetDepthMode(Camera camera) {
            if (camera == null) return;
            camera.depthTextureMode |= depthMode;
        }
#endif
    }
}
