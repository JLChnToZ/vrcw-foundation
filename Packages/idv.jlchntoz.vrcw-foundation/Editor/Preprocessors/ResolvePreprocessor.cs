using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using JLChnToZ.VRC.Foundation.Resolvers;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class ResolvePreprocessor : UdonSharpPreProcessor {
        static readonly Dictionary<FieldInfo, Resolver> resolveFields = new Dictionary<FieldInfo, Resolver>();
        readonly Dictionary<(FieldInfo, UnityObject), object> resolvedObjects = new Dictionary<(FieldInfo, UnityObject), object>();

        public override int Priority => -1; // Earlier than binding preprocessor.

        static Resolver ResolveField(FieldInfo field) {
            if (!resolveFields.TryGetValue(field, out var resolver)) {
                resolver = Resolver.Create();
                bool hasAttribute = false;
                foreach (var attribute in field.GetCustomAttributes<ResolveAttribute>(true)) {
                    var srcType = attribute.SourceType;
                    if (srcType != null) {
                        var srcPath = attribute.Source;
                        int hashIndex = srcPath.LastIndexOf("#");
                        if (hashIndex < 0) {
                            resolver.WithType(srcType);
                            resolver.WithPath(srcPath);
                        } else {
                            resolver.WithPath(srcPath.Substring(0, hashIndex));
                            resolver.WithType(srcType);
                            resolver.WithPath(srcPath.Substring(hashIndex + 1));
                        }
                    } else
                        resolver.WithPath(attribute.Source);
                    hasAttribute = true;
                }
                if (hasAttribute)
                    resolver.WithType(field.FieldType);
                else
                    resolver = null;
                resolveFields[field] = resolver;
            }
            return resolver;
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

        protected override void ProcessEntry(Type type, MonoBehaviour entry, UdonBehaviour udon) {
            bool changed = false;
            bool isAdaptor = entry is IUdonAdaptor;
            using (var so = new SerializedObject(entry)) {
                foreach (var field in GetFields<ResolveAttribute>(type)) {
                    try {
                        if (TryResolveUnchecked(field, entry, out var resolved) && !Equals(resolved, field.GetValue(entry))) {
                            so.FindProperty(field.Name).SetBoxedValue(resolved);
                            if (isAdaptor)
                                udon.publicVariables.TrySetVariableValue(field.Name, resolved);
                            changed = true;
                        }
                    } catch (Exception ex) {
                        Debug.LogError($"[ResolvePreprocessor] Unable to set `{field.Name}` in `{entry.name}`: {ex.Message}", entry);
                    }
                }
                if (!changed) return;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (entry is UdonSharpBehaviour usharp) UdonSharpEditorUtility.CopyProxyToUdon(usharp);
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
            var resolver = ResolveField(field);
            if (resolver != null) {
                if (resolvedObjects.TryGetValue((field, instance), out result)) return true;
                resolvedObjects[(field, instance)] = null; // Circular reference protection.
                if (resolver.TryResolve(instance, out result)) {
                    resolvedObjects[(field, instance)] = result;
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