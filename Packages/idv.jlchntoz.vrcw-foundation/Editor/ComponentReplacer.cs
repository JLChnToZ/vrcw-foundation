using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

using static UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    public class ComponentReplacer {
        static readonly Dictionary<Component, List<(Component, string)>> references = new Dictionary<Component, List<(Component, string)>>();
        static readonly HashSet<(Type, string)> blackListedPaths = new HashSet<(Type, string)>();
        static readonly Dictionary<Type, Type[]> dependents = new Dictionary<Type, Type[]>();
        readonly List<ComponentReplacer> downstreams = new List<ComponentReplacer>();
        readonly Type componentType;
        readonly GameObject sourceGameObject;
        GameObject temporaryGameObject;
        readonly Component[] componentsInGameObject;
        Component[] componentsInTemporary;
        readonly Component prefabComponent;
        readonly GameObject prefabInstance;
        readonly int componentIndex;

        public static void AddToBlackList(Type type, string path) => blackListedPaths.Add((type, path));

        public static T TryReplaceComponent<T>(Component oldComponent, bool copyContent) where T : Component {
            if (oldComponent == null) return null;
            var gameObject = oldComponent.gameObject;
            var components = gameObject.GetComponents<Component>();
            var index = Array.IndexOf(components, oldComponent);
            if (index < 0) {
                Debug.LogWarning($"Component {oldComponent.GetType()} is not found in the GameObject.");
                return null;
            }
            var replacer = new ComponentReplacer(gameObject, components, index);
            replacer.CloneToTemporary();
            replacer.DestroyDependents();
            var newComponent = gameObject.AddComponent<T>();
            if (copyContent) EditorUtility.CopySerializedIfDifferent(oldComponent, newComponent);
            replacer.RestoreDependents(newComponent);
            replacer.DestroyTemporary();
            return newComponent;
        }

        public static bool IsRequired(Type type, Type checkType, Type capableType = null) {
            if (!dependents.TryGetValue(type, out var types)) {
                var temp = new List<Type>();
                foreach (var requireComponent in type.GetCustomAttributes<RequireComponent>(true)) {
                    if (requireComponent.m_Type0 != null) temp.Add(requireComponent.m_Type0);
                    if (requireComponent.m_Type1 != null) temp.Add(requireComponent.m_Type1);
                    if (requireComponent.m_Type2 != null) temp.Add(requireComponent.m_Type2);
                }
                dependents[type] = types = temp.ToArray();
            }
            foreach (var t in types)
                if (t.IsAssignableFrom(checkType) && (capableType == null || !t.IsAssignableFrom(capableType)))
                    return true;
            return false;
        }

        public static void InitAllComponents() {
#if UNITY_2022_2_OR_NEWER
            int sceneCount = SceneManager.loadedSceneCount;
#else
            int sceneCount = SceneManager.sceneCount;
#endif
            var roots = new List<GameObject>();
            var temp = new List<Component>();
            var stack = new Stack<Transform>();
            for (int i = 0; i < sceneCount; i++) {
                SceneManager.GetSceneAt(i).GetRootGameObjects(roots);
                foreach (var root in roots)
                    stack.Push(root.transform);
            }
#if UNITY_2021_2_OR_NEWER
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                stack.Push(prefabStage.prefabContentsRoot.transform);
#endif
            while (stack.TryPop(out var current)) {
                for (int i = current.childCount - 1; i >= 0; i--)
                    stack.Push(current.GetChild(i));
                current.GetComponents(temp);
                foreach (var c in temp) {
                    if (c == null || c is Transform) continue;
                    using (var so = new SerializedObject(c)) {
                        var sp = so.GetIterator();
                        while (sp.Next(true)) {
                            if (sp.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var target = sp.objectReferenceValue as Component;
                            if (target == null || target == c) continue;
                            if (!references.TryGetValue(target, out var mapping))
                                references[target] = mapping = new List<(Component, string)>();
                            mapping.Add((c, sp.propertyPath));
                        }
                    }
                }
            }
        }

        public static ICollection<(Component, string)> GetReferencedComponents(Component component) =>
            component != null && references.TryGetValue(component, out var mapping) ?
            mapping as ICollection<(Component, string)> : Array.Empty<(Component, string)>();

        public static bool CanAllReferencesReplaceWith<T>(Component component) =>
            CanAllReferencesReplaceWith(component, typeof(T));

        public static bool CanAllReferencesReplaceWith(Component component, Type replaceType) {
            var referencedComponents = GetReferencedComponents(component);
            if (referencedComponents.Count == 0) return true;
            foreach (var (referencedBy, path) in referencedComponents) {
                if (referencedBy == null) continue;
                if (blackListedPaths.Contains((referencedBy.GetType(), path))) return false;
                using (var so = new SerializedObject(referencedBy)) {
                    var sp = so.FindProperty(path);
                    if (sp == null) continue;
                    Utils.GetFieldInfoFromProperty(sp, out var type);
                    if (type != null && !type.IsAssignableFrom(replaceType)) return false;
                }
            }
            return true;
        }

        ComponentReplacer(GameObject sourceGameObject, Component[] components, int index) {
            this.sourceGameObject = sourceGameObject;
            componentsInGameObject = components;
            componentIndex = index;
            var component = components[index];
            componentType = component.GetType();
            foreach (var c in componentsInGameObject) {
                if (c == null || c == component || !IsRequired(c.GetType(), componentType)) continue;
                int i = Array.IndexOf(componentsInGameObject, c);
                if (i >= 0) downstreams.Add(new ComponentReplacer(sourceGameObject, componentsInGameObject, i));
                else Debug.LogWarning($"Component {c.GetType()} is required by {componentType} but not found in the same GameObject.");
            }
            prefabComponent = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (prefabComponent != null)
                prefabInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(component);
        }

        void CloneToTemporary() {
            if (temporaryGameObject != null) return;
            temporaryGameObject = Instantiate(sourceGameObject);
            temporaryGameObject.hideFlags = HideFlags.HideAndDontSave;
            var queue = new Queue<ComponentReplacer>();
            queue.Enqueue(this);
            componentsInTemporary = temporaryGameObject.GetComponents<Component>();
            while (queue.TryDequeue(out var current)) {
                foreach (var downstream in current.downstreams) queue.Enqueue(downstream);
                current.componentsInTemporary = componentsInTemporary;
            }
        }

        void DestroyDependents() {
            var stack = new Stack<ComponentReplacer>();
            var queue = new Queue<ComponentReplacer>();
            queue.Enqueue(this);
            while (queue.TryDequeue(out var current)) {
                stack.Push(current);
                foreach (var downstream in current.downstreams) queue.Enqueue(downstream);
            }
            while (stack.TryPop(out var current)) {
                DestroyImmediate(current.componentsInGameObject[current.componentIndex]);
            }
        }

        void RestoreDependents(Component newAddedComponent = null) {
            var stack = new Stack<ComponentReplacer>();
            stack.Push(this);
            while (stack.TryPop(out var current)) {
                foreach (var downstream in current.downstreams) stack.Push(downstream);
                Component temp = null;
                if (current.componentIndex == componentIndex)
                    temp = newAddedComponent;
                else if (current.prefabComponent != null) {
                    PrefabUtility.RevertRemovedComponent(prefabInstance, current.prefabComponent, InteractionMode.AutomatedAction);
                    temp = current.componentsInGameObject[current.componentIndex];
                    if (temp == null) sourceGameObject.TryGetComponent(current.componentType, out temp);
                }
                if (temp == null) temp = sourceGameObject.AddComponent(current.componentType);
                current.componentsInGameObject[current.componentIndex] = temp;
                var src = current.componentsInTemporary[current.componentIndex];
                if (src == null) continue;
                if (temp != null && src.GetType() == temp.GetType()) EditorUtility.CopySerializedIfDifferent(src, temp);
                if (references.TryGetValue(src, out var mapping))
                    foreach (var (component, path) in mapping) {
                        using (var so = new SerializedObject(component))
                            try {
                                var sp = so.FindProperty(path);
                                sp.objectReferenceValue = temp;
                                so.ApplyModifiedProperties();
                            } catch { }
                    }
            }
        }

        void DestroyTemporary() {
            if (temporaryGameObject == null) return;
            DestroyImmediate(temporaryGameObject);
            temporaryGameObject = null;
            var queue = new Queue<ComponentReplacer>();
            queue.Enqueue(this);
            while (queue.TryDequeue(out var current)) {
                foreach (var downstream in current.downstreams) queue.Enqueue(downstream);
                current.componentsInTemporary = null;
            }
        }
    }
}
