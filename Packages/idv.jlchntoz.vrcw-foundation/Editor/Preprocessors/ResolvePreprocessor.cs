using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class ResolvePreprocessor : UdonSharpPreProcessor {
        readonly Dictionary<FieldInfo, ResolveAttribute[]> resolveFields = new Dictionary<FieldInfo, ResolveAttribute[]>();

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

        protected override void ProcessEntry(Type type, UdonSharpBehaviour usharp, UdonBehaviour udon) {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            using (var so = new SerializedObject(usharp)) {
                bool changed = false;
                foreach (var field in GetFields<ResolveAttribute>(type)) {
                    if (!resolveFields.TryGetValue(field, out var attributes))
                        resolveFields[field] = attributes = field.GetCustomAttributes<ResolveAttribute>(true).ToArray();
                    var srcObj = usharp as UnityObject;
                    var orgValue = field.GetValue(usharp) as UnityObject;
                    if (orgValue && attributes[0].NullOnly) continue;
                    bool hasResolutions = false;
                    for (int i = 0; i < attributes.Length; i++) {
                        if (!srcObj) break;
                        var srcPath = attributes[i].Source;
                        var srcType = attributes[i].SourceType ?? srcObj.GetType();
                        int hashIndex = srcPath.IndexOf('#');
                        if (hashIndex >= 0) {
                            srcObj = ResolvePath(srcPath.Substring(0, hashIndex), srcType == typeof(GameObject) ? typeof(Transform) : srcType, srcObj);
                            if (!srcObj) break;
                            srcPath = srcPath.Substring(hashIndex + 1);
                        }
                        srcObj = ResolveToType(srcObj, srcType);
                        if (!srcObj) break;
                        var targetField = srcType.GetField(srcPath, bindingFlags);
                        if (targetField != null) {
                            srcObj = targetField.GetValue(srcObj) as UnityObject;
                            hasResolutions = true;
                            continue;
                        }
                        var targetProperty = srcType.GetProperty(srcPath, bindingFlags);
                        if (targetProperty != null) {
                            srcObj = targetProperty.GetValue(srcObj) as UnityObject;
                            hasResolutions = true;
                            continue;
                        }
                    }
                    if (!hasResolutions) continue;
                    srcObj = ResolveToType(srcObj, field.FieldType);
                    if (orgValue != srcObj)
                        try {
                            so.FindProperty(field.Name).objectReferenceValue = srcObj;
                            changed = true;
                        } catch (Exception ex) {
                            Debug.LogError($"[ResolvePreprocessor] Unable to set `{field.Name}` in `{usharp.name}`: {ex.Message}", usharp);
                        }
                }
                if (changed) {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    UdonSharpEditorUtility.CopyProxyToUdon(usharp);
                }
            }
        }
    }
}