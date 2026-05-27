using System;
using UnityEngine;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityObject = UnityEngine.Object;
#endif
using JLChnToZ.VRC.Foundation.I18N;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// An editor component to help auto configurate VRChat interactable UI canvas.
    /// </summary>
    [ExecuteInEditMode, EditorOnly, DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("JLChnToZ VRCW Foundation/VRC Canvas Configurator")]
    public class VRCCanvasConfigurator : MonoBehaviour {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        [SerializeField, LocalizedLabel, LocalizedEnum] Axis fixedAxis = Axis.Horizontal;
        [SerializeField, LocalizedLabel] bool fixCollider = true;
        new RectTransform transform;
        new BoxCollider collider;
        [NonSerialized] bool scaleChanging;
        [NonSerialized] Vector2 lastLocalScale;

        static float GetGlobalAspect(Transform transform, Vector2 size) {
            if (transform == null) return size.x / size.y;
            Span<Vector3> v = stackalloc Vector3[2];
            v[0].x = size.x;
            v[1].y = size.y;
            transform.TransformVectors(v);
            return v[0].magnitude / v[1].magnitude;
        }

        static void SavePrefabChanges(UnityObject obj) {
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);   
        }

        void Update() {
            if (AnimationMode.InAnimationMode() || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FixUpTransform(out var localScale))
                SavePrefabChanges(transform);
            if (FixUpCollider())
                SavePrefabChanges(collider);
            lastLocalScale = localScale;
        }

        bool FixUpTransform(out Vector2 localScale) {
            if (transform == null) {
                if (!TryGetComponent(out transform)) {
                    localScale = default;
                    return false;
                }
                lastLocalScale = localScale = transform.localScale;
            } else
                localScale = transform.localScale;
            if (localScale != lastLocalScale) {
                scaleChanging = true;
                return false;
            }
            if (scaleChanging) {
                scaleChanging = false;
                return false;
            }
            return transform.NormalizeDimensions(fixedAxis);
        }

        bool FixUpCollider() {
            if (!fixCollider) return false;
            if (collider == null && !TryGetComponent(out collider)) {
                Undo.RecordObject(this, "Fixup Canvas");
                fixCollider = false;
                SavePrefabChanges(this);
                return false;
            }
            return transform.SynchronizeColliderWithRect(collider);
        }
#endif
    }
}
