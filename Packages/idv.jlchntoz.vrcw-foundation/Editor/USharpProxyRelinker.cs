using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class USharpProxyRelinker {

        [InitializeOnLoadMethod]
        private static void OnLoad() {
            EditorApplication.delayCall += RelinkProxies;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            EditorApplication.delayCall += RelinkProxies;
        }

        private static void RelinkProxies() {
            foreach (var usharp in Object.FindObjectsOfType<UdonSharpBehaviour>(true)) {
                using var so = new SerializedObject(usharp);
                var backingField = so.FindProperty("_udonSharpBackingUdonBehaviour");
                if (backingField == null || !backingField.prefabOverride) continue;
                var backing = backingField.objectReferenceValue;
                if (backing != null && backing is UdonBehaviour) continue;
                Debug.LogWarning($"Found broken U# proxy on {usharp.name}, it seems been overridden by invalid reference.", usharp);
                PrefabUtility.RevertPropertyOverride(backingField, InteractionMode.AutomatedAction);
                EditorUtility.SetDirty(usharp);
            }
        }
    }
}
