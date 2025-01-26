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
        readonly HashSet<UnityObject> resolvingObjects = new HashSet<UnityObject>();

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
                resolvingObjects.Clear();
                resolvedObjects.Clear();
            }
        }

        protected override void ProcessEntry(Type type, UdonSharpBehaviour usharp, UdonBehaviour udon) {
            bool changed = false;
            using (var so = new SerializedObject(usharp)) {
                foreach (var field in GetFields<ResolveAttribute>(type)) {
                    if (!resolveFields.TryGetValue(field, out var attributes))
                        resolveFields[field] = attributes = field.GetCustomAttributes<ResolveAttribute>(true).ToArray();
                    try {
                        var orgValue = field.GetValue(usharp) as UnityObject;
                        var srcObj = Resolve(field, attributes, usharp);
                        if (orgValue != srcObj)
                            so.FindProperty(field.Name).objectReferenceValue = srcObj;
                        changed = true;
                    } catch (Exception ex) {
                        Debug.LogError($"[ResolvePreprocessor] Unable to set `{field.Name}` in `{usharp.name}`: {ex.Message}", usharp);
                    }
                }
                if (!changed) return;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            UdonSharpEditorUtility.CopyProxyToUdon(usharp);
        }

        UnityObject Resolve(FieldInfo field, ResolveAttribute[] attributes, UnityObject instance) {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            if (!resolvingObjects.Add(instance))
                throw new InvalidOperationException($"Circular reference detected on `{instance.name}`.");
            try {
                if (resolvedObjects.TryGetValue((field, instance), out var result))
                    return result;
                var srcObj = instance;
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
                        if (resolveFields.TryGetValue(targetField, out var subAttributes))
                            srcObj = Resolve(targetField, subAttributes, srcObj);
                        else
                            srcObj = targetField.GetValue(srcObj) as UnityObject;
                        continue;
                    }
                    var targetProperty = srcType.GetProperty(srcPath, bindingFlags);
                    if (targetProperty != null) {
                        srcObj = targetProperty.GetValue(srcObj) as UnityObject;
                        continue;
                    }
                }
                srcObj = ResolveToType(srcObj, field.FieldType);
                resolvedObjects[(field, instance)] = srcObj;
                return srcObj;
            } finally {
                resolvingObjects.Remove(instance);
            }
        }
    }
}