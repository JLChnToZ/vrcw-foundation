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
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class ResolvePreprocessor : UdonSharpPreProcessor {
        readonly Dictionary<FieldInfo, ResolveAttribute[]> resolveFields = new Dictionary<FieldInfo, ResolveAttribute[]>();
        readonly Dictionary<(FieldInfo, UnityObject), UnityObject> resolvedObjects = new Dictionary<(FieldInfo, UnityObject), UnityObject>();

        public override int Priority => -1; // Earlier than binding preprocessor.

        static UnityObject ResolveToType(UnityObject src, Type destType) {
            if (!src) return null;
            if (destType == null || destType.IsInstanceOfType(src)) return src;
            if (src is GameObject gameObject) {
                if (typeof(Component).IsAssignableFrom(destType) &&
                    gameObject.TryGetComponent(destType, out var result))
                    return result;
            } else if (src is Component component) {
                if (destType == typeof(GameObject))
                    return component.gameObject;
                if (typeof(Component).IsAssignableFrom(destType) &&
                    component.TryGetComponent(destType, out var result))
                    return result;
            }
            return null; // Unable to resolve.
        }

        public override void OnPreprocess(Scene scene) {
            try {
                base.OnPreprocess(scene);
            } finally {
                resolvedObjects.Clear();
            }
        }

        protected override void ProcessEntry(Type type, UdonSharpBehaviour usharp, UdonBehaviour udon) {
            bool changed = false;
            using (var so = new SerializedObject(usharp)) {
                foreach (var field in GetFields<ResolveAttribute>(type)) {
                    try {
                        if (TryResolve(field, usharp, out var resolved) && field.GetValue(usharp) as UnityObject != resolved) {
                            so.FindProperty(field.Name).objectReferenceValue = resolved;
                            changed = true;
                        }
                    } catch (Exception ex) {
                        Debug.LogError($"[ResolvePreprocessor] Unable to set `{field.Name}` in `{usharp.name}`: {ex.Message}", usharp);
                    }
                }
                if (!changed) return;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            UdonSharpEditorUtility.CopyProxyToUdon(usharp);
        }

        bool TryResolve(FieldInfo field, UnityObject instance, out UnityObject result) {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            if (!resolveFields.TryGetValue(field, out var attributes)) {
                attributes = field.GetCustomAttributes<ResolveAttribute>(true).ToArray();
                resolveFields[field] = attributes.Length > 0 ? attributes : null;
            }
            if (attributes != null) {
                if (resolvedObjects.TryGetValue((field, instance), out result)) return true;
                resolvedObjects[(field, instance)] = null; // Circular reference protection.
                result = instance;
                bool hasResolved = false;
                for (int i = 0; i < attributes.Length; i++) {
                    if (!result) break;
                    var srcPath = attributes[i].Source;
                    var srcType = attributes[i].SourceType ?? result.GetType();
                    int hashIndex = srcPath.IndexOf('#');
                    if (hashIndex >= 0) {
                        result = ResolvePath(srcPath.Substring(0, hashIndex), srcType == typeof(GameObject) ? typeof(Transform) : srcType, result);
                        if (!result) break;
                        srcPath = srcPath.Substring(hashIndex + 1);
                    }
                    result = ResolveToType(result, srcType);
                    if (!result) break;
                    if (srcType == typeof(UdonBehaviour)) {
                        result = (result as UdonBehaviour).GetProgramVariable(srcPath) as UnityObject;
                        hasResolved = true;
                        continue;
                    }
                    var targetField = srcType.GetField(srcPath, bindingFlags);
                    if (targetField != null) {
                        if (TryResolve(targetField, result, out var resolved))
                            result = resolved;
                        else
                            result = targetField.GetValue(result) as UnityObject;
                        hasResolved = true;
                        continue;
                    }
                    var targetProperty = srcType.GetProperty(srcPath, bindingFlags);
                    if (targetProperty != null) {
                        result = targetProperty.GetValue(result) as UnityObject;
                        hasResolved = true;
                        continue;
                    }
                }
                if (hasResolved) {
                    resolvedObjects[(field, instance)] = result = ResolveToType(result, field.FieldType);
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}