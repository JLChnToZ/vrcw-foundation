using UnityEngine;
using UnityEditor;
using UdonSharp;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    [CustomPropertyDrawer(typeof(ResolveAttribute))]
    public class ResolveAttributeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            switch (TryReolveProperty(property, true, out var result)) {
                case VisibilityState.Hidden:
                    return;
                case VisibilityState.Resolved:
                    using (new EditorGUI.DisabledScope(true))
                    using (var prop = new EditorGUI.PropertyScope(position, label, property))
                        EditorGUI.ObjectField(position, prop.content, result, typeof(UnityObject), true);
                    return;
                default:
                    EditorGUI.PropertyField(position, property, label);
                    break;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            switch (TryReolveProperty(property, false, out var _)) {
                case VisibilityState.Hidden:
                    return 0;
                case VisibilityState.Resolved:
                    return EditorGUIUtility.singleLineHeight;
                default:
                    return EditorGUI.GetPropertyHeight(property, label);
            }
        }

        VisibilityState TryReolveProperty(SerializedProperty property, bool resultRequired, out UnityObject result) {
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) {
                result = null;
                return property == null ? VisibilityState.Hidden : VisibilityState.Visible;
            }
            result = property.objectReferenceValue;
            var attr = attribute as ResolveAttribute;
            if (attr == null) return VisibilityState.Visible;
            var target = property.serializedObject.targetObject as UdonSharpBehaviour;
            if (target == null) return VisibilityState.Visible;
            var fieldInfo = Utils.GetFieldInfoFromProperty(property, out var _);
            if (result && attr.NullOnly) return VisibilityState.Visible;
            bool hide = attr.HideInInspectorIfResolvable;
            if (!hide && !resultRequired) return VisibilityState.Visible;
            return new ResolvePreprocessor().TryResolve(fieldInfo, target, out result) && result ? hide ?
                VisibilityState.Hidden :
                VisibilityState.Resolved :
                VisibilityState.Visible;
        }

        enum VisibilityState {
            Visible,
            Hidden,
            Resolved,
        }
    }
}