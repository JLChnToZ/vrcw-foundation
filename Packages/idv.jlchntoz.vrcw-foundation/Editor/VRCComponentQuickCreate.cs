using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using VRC.SDK3.Components;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class VRCComponentQuickCreate {
        const string MENU_ROOT = "GameObject/JLChnToZ VRCW Foundation/";

        [MenuItem(MENU_ROOT + "VRChat World Canvas", false, 10)]
        public static void CreateCanvas() {
            var selectedTransform = Selection.activeTransform;
            var go = new GameObject(
                GameObjectUtility.GetUniqueNameForSibling(selectedTransform, "Interactable Canvas"),
                typeof(RectTransform),
                typeof(VRCCanvasConfigurator),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(VRCUiShape),
                typeof(BoxCollider)
            ) {
                layer = 0,
            };
            if (go.TryGetComponent(out Canvas canvas)) {
                canvas.renderMode = RenderMode.WorldSpace;
            }
            if (go.TryGetComponent(out RectTransform transform)) {
                transform.localScale = Vector3.one * 0.001F;
                transform.sizeDelta = new Vector2(1000F, 1000F);
                transform.SetParent(selectedTransform, false);
            }
            if (go.TryGetComponent(out CanvasScaler scaler)) {
                scaler.dynamicPixelsPerUnit = 10F;
                scaler.referencePixelsPerUnit = 100F;
            }
            if (go.TryGetComponent(out BoxCollider collider)) {
                collider.isTrigger = true;
                if (transform) {
                    var rect = transform.rect;
                    collider.center = rect.center;
                    collider.size = rect.size;
                }
            }
            Undo.RegisterCreatedObjectUndo(go, "Create VRChat World Canvas");
            Selection.activeGameObject = go;
        }
    }
}
