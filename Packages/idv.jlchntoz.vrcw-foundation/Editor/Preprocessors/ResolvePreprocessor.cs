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
using JLChnToZ.VRC.Foundation.Resolvers;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class ResolvePreprocessor : UdonSharpPreProcessor {
        static readonly Dictionary<FieldInfo, ResolveAttribute[]> resolveFields = new Dictionary<FieldInfo, ResolveAttribute[]>();
        readonly Dictionary<(FieldInfo, UnityObject), object> resolvedObjects = new Dictionary<(FieldInfo, UnityObject), object>();

        public override int Priority => -1; // Earlier than binding preprocessor.

        static object ResolveToType(object src, Type destType) {
            if (!src.IsValid()) return null;
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

        static ResolveAttribute[] ResolveField(FieldInfo field) {
            if (!resolveFields.TryGetValue(field, out var attributes)) {
                attributes = field.GetCustomAttributes<ResolveAttribute>(true).ToArray();
                if (attributes.Length == 0) attributes = null;
                resolveFields[field] = attributes;
            }
            return attributes;
        }

        public override void OnPreprocess(Scene scene) {
            try {
                Resolver.CustomResolveProvider += TryResolveHook;
                base.OnPreprocess(scene);
            } finally {
                resolvedObjects.Clear();
                Resolver.CustomResolveProvider -= TryResolveHook;
            }
        }

        protected override void ProcessEntry(Type type, UdonSharpBehaviour usharp, UdonBehaviour udon) {
            bool changed = false;
            using (var so = new SerializedObject(usharp)) {
                foreach (var field in GetFields<ResolveAttribute>(type)) {
                    try {
                        if (TryResolveUnchecked(field, usharp, out var resolved) && !Equals(resolved, field.GetValue(usharp))) {
                            so.FindProperty(field.Name).SetBoxedValue(resolved);
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

        public bool TryResolve(FieldInfo field, UnityObject instance, out object result) {
            try {
                Resolver.CustomResolveProvider += TryResolveHook;
                return TryResolveUnchecked(field, instance, out result);
            } finally {
                Resolver.CustomResolveProvider -= TryResolveHook;
            }
        }

        bool TryResolveHook(object source, string memberName, MemberInfo member, out object result) {
            if (member is FieldInfo fieldInfo && source is UnityObject unityObject)
                return TryResolveUnchecked(fieldInfo, unityObject, out result);
            result = null;
            return false;
        }

        bool TryResolveUnchecked(FieldInfo field, UnityObject instance, out object result) {
            if (!instance.IsValid()) {
                result = null;
                return false;
            }
            var attributes = ResolveField(field);
            if (attributes != null) {
                if (resolvedObjects.TryGetValue((field, instance), out result)) return true;
                resolvedObjects[(field, instance)] = null; // Circular reference protection.
                result = instance;
                bool hasResolved = false;
                for (int i = 0; i < attributes.Length; i++) {
                    if (!result.IsValid()) break;
                    var srcPath = attributes[i].Source;
                    var srcType = attributes[i].SourceType ?? result.GetType();
                    if (new Resolver(srcPath, srcType).TryResolve(result, out result))
                        hasResolved = true;
                }
                if (hasResolved) {
                    resolvedObjects[(field, instance)] = result = ResolveToType(result, field.FieldType);
                    return true;
                }
            }
            result = null;
            return false;
        }

        public void ClearCache() {
            resolvedObjects.Clear();
        }
    }
}