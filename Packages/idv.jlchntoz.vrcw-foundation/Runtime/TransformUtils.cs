using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if COMPILER_UDONSHARP
using VRC.SDKBase;
#endif

namespace JLChnToZ.VRC.Foundation {
    public static class TransformUtils {
        public static float GetGlobalAspect(Transform transform, Vector2 size) {
#if COMPILER_UDONSHARP
            return Utilities.IsValid(transform) ? Mathf.Sqrt(
                transform.TransformVector(size.x, 0F, 0F).sqrMagnitude /
                transform.TransformVector(0F, size.y, 0F).sqrMagnitude
            ) : size.x / size.y;
#else
            if (transform == null) return size.x / size.y;
            Span<Vector3> v = stackalloc Vector3[2];
            v[0].x = size.x;
            v[1].y = size.y;
            transform.TransformVectors(v);
            return Mathf.Sqrt(v[0].sqrMagnitude / v[1].sqrMagnitude);
#endif
        }

        public static bool NormalizeDimensions(this RectTransform transform, Axis axis, bool undo = true) {
            var localScale = transform.localScale;
            var localSize = transform.sizeDelta;
            float targetAspect = GetGlobalAspect(transform, localSize);
            var parentAspect = GetGlobalAspect(transform.parent, localScale);
            if (Mathf.Approximately(parentAspect, 1F)) return false;
            switch (axis) {
                case Axis.Horizontal: localScale.y *= parentAspect; break;
                case Axis.Vertical: localScale.x /= parentAspect; break;
                default: return false;
            }
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (undo) Undo.RecordObject(transform, "Normalize Canvas Transform");
#endif
            localScale.z = Mathf.Sqrt(Mathf.Abs(localScale.x * localScale.y));
            transform.localScale = localScale;
            if (Mathf.Approximately(targetAspect, GetGlobalAspect(transform, localSize))) return true;
            switch (axis) {
                case Axis.Horizontal: localSize.y = localSize.x / targetAspect; break;
                case Axis.Vertical: localSize.x = localSize.y * targetAspect; break;
                default: return true;
            }
            transform.sizeDelta = localSize;
            return true;
        }

        public static bool SynchronizeColliderWithRect(this RectTransform transform, BoxCollider collider, bool undo = true) {
            var rect = transform.rect;
            var scale = transform.lossyScale.sqrMagnitude;
            Vector3 orgScale = collider.size;
            Vector3 newScale = rect.size;
            Vector3 newCenter = rect.center;
            newScale.z = Mathf.Approximately(scale, 0F) ? 1F : 0.001F / Mathf.Sqrt(scale);
            newCenter.z = newScale.z * 0.5F;
            if (collider.isTrigger && collider.center == newCenter && orgScale == newScale)
                return false;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            if (undo) Undo.RecordObject(collider, "Fixup Canvas Collider");
#endif
            collider.isTrigger = true;
            collider.center = newCenter;
            collider.size = newScale;
            return true;
        }

#if !COMPILER_UDONSHARP
        public static ComponentsEnumerable<T> IterateAllComponents<T>(this Scene scene, bool includeEditorOnly = false) where T : Component {
            var stackPoolRef = PooledObjectExtensions.Get(out Stack<Transform> pending);
            var rootGameObjects = scene.GetRootGameObjects();
            for (int i = rootGameObjects.Length - 1; i >= 0; i--) pending.Push(rootGameObjects[i].transform);
            return new ComponentsEnumerable<T>(stackPoolRef, pending, includeEditorOnly);
        }

        public static ComponentsEnumerable<T> IterateAllComponents<T>(this GameObject gameObject, bool includeEditorOnly = false) where T : Component {
            var stackPoolRef = PooledObjectExtensions.Get(out Stack<Transform> pending);
            pending.Push(gameObject.transform);
            return new ComponentsEnumerable<T>(stackPoolRef, pending, includeEditorOnly);
        }

        public static ComponentsEnumerable<T> IterateAllComponents<T>(Stack<Transform> pending, bool includeEditorOnly) where T : Component =>
            new(null, pending, includeEditorOnly);

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

        public readonly struct ComponentsEnumerable<T> : IEnumerable<T> where T : Component {
            readonly IDisposable stackPoolRef;
            readonly Stack<Transform> pending;
            readonly bool includeEditorOnly;

            internal ComponentsEnumerable(IDisposable stackPoolRef, Stack<Transform> pending, bool includeEditorOnly) {
                this.stackPoolRef = stackPoolRef;
                this.pending = pending;
                this.includeEditorOnly = includeEditorOnly;
            }

            public Enumerator GetEnumerator() => new(stackPoolRef, pending, includeEditorOnly);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<T> {
                readonly IDisposable stackPoolRef;
                readonly bool includeEditorOnly;
                readonly List<T> components;
                readonly Stack<Transform> pending;
                int index;

                public readonly T Current => components[index];

                readonly object IEnumerator.Current => components[index];

                internal Enumerator(IDisposable stackPoolRef, Stack<Transform> pending, bool includeEditorOnly) {
                    this.stackPoolRef = stackPoolRef;
                    this.includeEditorOnly = includeEditorOnly;
                    this.pending = pending;
                    components = ListPool<T>.Get();
                    index = -1;
                }

                public bool MoveNext() {
                    index++;
                    while (index >= components.Count) {
                        if (!pending.TryPop(out var transform)) return false;
                        if (transform == null || (!includeEditorOnly && transform.CompareTag("EditorOnly"))) continue;
                        for (int i = transform.childCount - 1; i >= 0; i--) pending.Push(transform.GetChild(i));
                        transform.GetComponents(components);
                        index = 0;
                    }
                    return true;
                }

                public void Reset() => throw new NotSupportedException();

                public readonly void Dispose() {
                    try {
                        if (components != null) ListPool<T>.Release(components);
                    } catch { }
                    try {
                        stackPoolRef?.Dispose();
                    } catch { }
                }
            }
        }
#endif
    }

    public enum Axis {
        Horizontal = 0,
        Vertical = 1,
    }
}