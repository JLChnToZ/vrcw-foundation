using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class VRCComponentQuickCreate {
        const string MENU_ROOT = "GameObject/Vistanz Foundation/";

        [MenuItem(MENU_ROOT + "VRChat World Canvas", false, 10)]
        public static void CreateCanvas() {
            var selectedTransform = Selection.activeTransform;
            var go = new GameObject(
                GameObjectUtility.GetUniqueNameForSibling(selectedTransform, "Canvas"),
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(VRCUiShape),
                typeof(BoxCollider)
            ) {
                layer = 0
            };
            if (go.TryGetComponent(out Canvas canvas)) {
                canvas.renderMode = RenderMode.WorldSpace;
            }
            if (go.TryGetComponent(out RectTransform transform)) {
                transform.localScale = Vector3.one * 0.001F;
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
        }
    }
}
