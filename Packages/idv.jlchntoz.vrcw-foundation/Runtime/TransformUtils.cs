using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation {
    public static class TransformUtils {
#if !COMPILER_UDONSHARP
        public static IEnumerable<T> IterateAllComponents<T>(this Scene scene, bool includeEditorOnly = false) where T : Component {
            using (PooledObjectExtensions.Get(out Stack<Transform> pending)) {
                var rootGameObjects = scene.GetRootGameObjects();
                for (int i = rootGameObjects.Length - 1; i >= 0; i--) pending.Push(rootGameObjects[i].transform);
                return IterateAllComponents<T>(pending, includeEditorOnly);
            }
        }

        public static IEnumerable<T> IterateAllComponents<T>(this GameObject gameObject, bool includeEditorOnly = false) where T : Component {
            using (PooledObjectExtensions.Get(out Stack<Transform> pending)) {
                pending.Push(gameObject.transform);
                return IterateAllComponents<T>(pending, includeEditorOnly);
            }
        }

        public static IEnumerable<T> IterateAllComponents<T>(Stack<Transform> pending, bool includeEditorOnly) where T : Component {
            while (pending.TryPop(out var transform)) {
                if (transform == null || (!includeEditorOnly && transform.CompareTag("EditorOnly"))) continue;
                for (int i = transform.childCount - 1; i >= 0; i--) pending.Push(transform.GetChild(i));
                using (PooledObjectExtensions.Get(out List<T> components)) {
                    transform.GetComponents(components);
                    foreach (var component in components) if (component != null) yield return component;
                }
            }
        }

        public static bool IsAvailableOnRuntime(this UnityObject gameObjectOrComponent) {
            if (gameObjectOrComponent == null) return false;
            for (var transform =
                gameObjectOrComponent is Transform t ? t :
                gameObjectOrComponent is GameObject go ? go.transform :
                gameObjectOrComponent is Component c ? c.transform :
                null;
                transform != null; transform = transform.parent)
                if (transform.CompareTag("EditorOnly")) return false;
            return true;
        }

        public static T FindClosestComponentInHierarchy<T>(Transform startFrom, GameObject[] roots = null) where T : Component =>
            FindClosestComponentInHierarchy(startFrom, typeof(T), roots) as T;

        public static Component FindClosestComponentInHierarchy(Transform startFrom, Type type, GameObject[] roots = null) {
            for (Transform transform = startFrom, lastTransform = null; transform != null; transform = transform.parent) {
                if (transform.TryGetComponent(type, out var result)) return result;
                foreach (Transform child in transform) {
                    if (lastTransform == child) continue;
                    result = transform.GetComponentInChildren(type, true);
                    if (result != null) return result;
                }
                lastTransform = transform;
            }
            if (roots == null) {
                var scene = startFrom.gameObject.scene;
                if (!scene.IsValid()) return null;
                roots = scene.GetRootGameObjects();
            }
            foreach (var root in roots) {
                var result = root.GetComponentInChildren(type, true);
                if (result != null) return result;
            }
            return null;
        }
#endif
    }
}