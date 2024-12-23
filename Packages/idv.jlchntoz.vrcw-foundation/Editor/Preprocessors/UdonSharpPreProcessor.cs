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

        protected static UnityObject ResolvePath(string path, Type srcType, UnityObject source) {
            var pathElements = path.Split('/');
            if (source is Transform transform) {}
            else if (source is Component component)
                transform = component.transform;
            else if (source is GameObject gameObject)
                transform = gameObject.transform;
            else return null;
            source = null;
            var pending = new Stack<(int, Transform)>();
            var children = new Stack<Transform>();
            var flattenChildren = new Stack<Transform>();
            pending.Push((0, transform));
            while (pending.Count > 0) {
                var (depth, current) = pending.Pop();
                if (depth >= pathElements.Length) {
                    if (srcType.IsAssignableFrom(current.GetType())) {
                        source = current;
                        break;
                    } else if (current.TryGetComponent(srcType, out var component)) {
                        source = component;
                        break;
                    }
                    continue;
                }
                var pathPart = pathElements[depth];
                switch (pathPart) {
                    case "": {
                        var next = depth == 0 ? current.root : current;
                        if (next != null) pending.Push((depth + 1, next));
                        break;
                    }
                    case ".": {
                        pending.Push((depth + 1, current));
                        break;
                    }
                    case "..": {
                        var parent = current.parent;
                        if (parent != null) pending.Push((depth + 1, parent));
                        break;
                    }
                    case "..*": {
                        for (var parent = current; parent != null; parent = parent.parent)
                            flattenChildren.Push(parent);
                        while (flattenChildren.Count > 0)
                            pending.Push((depth + 1, flattenChildren.Pop()));
                        break;
                    }
                    case "*": {
                        for (int i = current.childCount - 1; i >= 0; i--)
                            pending.Push((depth + 1, current.GetChild(i)));
                        break;
                    }
                    case "**": {
                        children.Push(current);
                        while (children.Count > 0) {
                            var child = children.Pop();
                            flattenChildren.Push(child);
                            for (int i = 0, count = child.childCount; i < count; i++)
                                children.Push(child.GetChild(i));
                        }
                        while (flattenChildren.Count > 0)
                            pending.Push((depth + 1, flattenChildren.Pop()));
                        break;
                    }
                    default: {
                        var child = current.Find(pathPart);
                        if (child != null) pending.Push((depth + 1, child));
                        break;
                    }
                }
            }
            return source;
        }

        protected static bool TryGetValue(UnityObject source, Type srcType, string fieldName, out object result) {
            if (source == null) {
                result = null;
                return false;
            }
            var otherProp = srcType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (otherProp != null) {
                result = otherProp.GetValue(source);
                return true;
            }
            var otherField = srcType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (otherField != null) {
                result = otherField.GetValue(source);
                return true;
            }
            result = null;
            return false;
        }

        protected static void GatherMonoScripts() {
            scriptMap.Clear();
            foreach (var script in AssetDatabase.FindAssets("t:MonoScript").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MonoScript>)) {
                var type = script.GetClass();
                if (type != null) scriptMap[type] = script;
            }
        }
    }
}
