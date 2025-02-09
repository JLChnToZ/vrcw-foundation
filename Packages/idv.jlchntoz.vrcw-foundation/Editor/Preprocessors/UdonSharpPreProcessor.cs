using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal abstract class UdonSharpPreProcessor : IPreprocessor {
        protected static readonly Dictionary<Type, MonoScript> scriptMap = new Dictionary<Type, MonoScript>();
        readonly Dictionary<Type, FieldInfo[]> filteredFields = new Dictionary<Type, FieldInfo[]>();

        public virtual int Priority => 0;

        public virtual void OnPreprocess(Scene scene) {
            foreach (var usharp in scene.IterateAllComponents<UdonSharpBehaviour>()) {
                var type = usharp.GetType();
                var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(usharp);
                if (udon == null) {
                    Debug.LogError($"[{GetType().Name}] `{usharp.name}` is not correctly configured.", usharp);
                    continue;
                }
                ProcessEntry(type, usharp, udon);
            }
        }

        protected virtual void ProcessEntry(Type type, UdonSharpBehaviour proxy, UdonBehaviour udon) {}

        protected FieldInfo[] GetFields<T>(Type type) {
            if (!filteredFields.TryGetValue(type, out var fieldInfos)) {
                fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                    .Where(
                        typeof(Attribute).IsAssignableFrom(typeof(T)) ?
                        IsAttributeDefined<T> :
                        IsAssignable<T>
                    ).ToArray();
                filteredFields[type] = fieldInfos;
            }
            return fieldInfos;
        }

        static bool IsAttributeDefined<T>(FieldInfo field) => field.IsDefined(typeof(T), true);

        static bool IsAssignable<T>(FieldInfo field) => typeof(T).IsAssignableFrom(field.FieldType);

        protected static void GatherMonoScripts() {
            scriptMap.Clear();
            foreach (var script in AssetDatabase.FindAssets("t:MonoScript").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MonoScript>)) {
                var type = script.GetClass();
                if (type != null) scriptMap[type] = script;
            }
        }
    }
}
