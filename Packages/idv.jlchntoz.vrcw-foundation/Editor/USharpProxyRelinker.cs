using UnityEditor;
using UnityEngine;
using VRC.Udon;
using UdonSharp;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class USharpProxyRelinker {
        static readonly FieldInfo backingDumpFieldInfo = typeof(UdonSharpBehaviour).GetField(
            "_backingUdonBehaviourDump",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

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
                if (!PrefabUtility.IsPartOfPrefabInstance(usharp)) continue;
                using var so = new SerializedObject(usharp);
                var backingField = so.FindProperty("_udonSharpBackingUdonBehaviour");
                if (backingField == null || !backingField.prefabOverride) continue;
                var backing = backingField.objectReferenceValue;
                if (backing != null) continue;
                PrefabUtility.RevertPropertyOverride(backingField, InteractionMode.AutomatedAction);
                so.Update();
                backing = backingField.objectReferenceValue;
                backingDumpFieldInfo.SetValue(usharp, backing);
                if (backing != null) Debug.Log($"Successfully restored UdonSharp backing UdonBehaviour for {usharp.name}", usharp);
                else Debug.LogWarning($"Failed to restore UdonSharp backing UdonBehaviour for {usharp.name}", usharp);
                EditorUtility.SetDirty(usharp);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(usharp);
            }
        }
    }
}
