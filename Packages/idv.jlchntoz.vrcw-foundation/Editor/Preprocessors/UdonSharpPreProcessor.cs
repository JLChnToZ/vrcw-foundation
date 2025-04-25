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
            foreach (var entry in scene.IterateAllComponents<MonoBehaviour>()) {
                if (entry is IUdonAdaptor adaptor) {
                    var udon = adaptor.TargetBehaviour;
                    if (udon == null) {
                        var us = adaptor.TargetUdonSharpBehaviour;
                        if (us != null) udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(us);
                    }
                    ProcessEntry(entry, udon);
                }
                if (entry is UdonSharpBehaviour usharp)
                    ProcessEntry(entry, UdonSharpEditorUtility.GetBackingUdonBehaviour(usharp));
            }
        }

        void ProcessEntry(MonoBehaviour entry, UdonBehaviour udon) {
            if (udon == null) {
                Debug.LogError($"[{GetType().Name}] `{entry.name}` is not correctly configured.", entry);
                return;
            }
            var type = entry.GetType();
            ProcessEntry(type, entry, udon);
        }

        protected virtual void ProcessEntry(Type type, MonoBehaviour entry, UdonBehaviour udon) {}

        protected FieldInfo[] GetFields<T>(Type type) {
            if (!filteredFields.TryGetValue(type, out var fieldInfos)) {
                var fieldList = new List<FieldInfo>();
                Func<FieldInfo, bool> filter = typeof(Attribute).IsAssignableFrom(typeof(T)) ? IsAttributeDefined<T> : IsAssignable<T>;
                for (var t = type; t != null && t != typeof(object); t = t.BaseType) {
                    if (filteredFields.TryGetValue(t, out fieldInfos)) {
                        fieldList.AddRange(fieldInfos);
                        break;
                    }
                    fieldList.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(filter));
                }
                filteredFields[type] = fieldInfos = fieldList.ToArray();
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
