using UnityEngine;
using UnityEditor;
using UdonSharp;
using UnityObject = UnityEngine.Object;
using System;

namespace JLChnToZ.VRC.Foundation.Editors {
    [CustomPropertyDrawer(typeof(ResolveAttribute))]
    public class ResolveAttributeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            switch (TryReolveProperty(property, true, out var result)) {
                case VisibilityState.Hidden:
                    return;
                case VisibilityState.Resolved:
                    using (new EditorGUI.DisabledScope(true))
                    using (var prop = new EditorGUI.PropertyScope(position, label, property)) {
                        if (result == null) {
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                                EditorGUI.ObjectField(position, prop.content, null, typeof(UnityObject), true);
                            else
                                EditorGUI.LabelField(position, prop.content, Utils.GetTempContent("null"));
                        } else if (result is UnityObject unityObj)
                            EditorGUI.ObjectField(position, prop.content, unityObj, typeof(UnityObject), true);
                        else if (result is Vector2 v2)
                            EditorGUI.Vector2Field(position, prop.content, v2);
                        else if (result is Vector3 v3)
                            EditorGUI.Vector3Field(position, prop.content, v3);
                        else if (result is Vector4 v4)
                            EditorGUI.Vector4Field(position, prop.content, v4);
                        else if (result is Quaternion q)
                            EditorGUI.Vector4Field(position, prop.content, new Vector4(q.x, q.y, q.z, q.w));
                        else if (result is Color c)
                            EditorGUI.ColorField(position, prop.content, c);
                        else if (result is Color32 sc)
                            EditorGUI.ColorField(position, prop.content, sc);
                        else if (result is Rect r)
                            EditorGUI.RectField(position, prop.content, r);
                        else if (result is RectInt ri)
                            EditorGUI.RectIntField(position, prop.content, ri);
                        else if (result is Bounds b)
                            EditorGUI.BoundsField(position, prop.content, b);
                        else if (result is BoundsInt bi)
                            EditorGUI.BoundsIntField(position, prop.content, bi);
                        else if (result is LayerMask lm)
                            EditorGUI.LayerField(position, prop.content, lm);
                        else if (result is AnimationCurve ac)
                            EditorGUI.CurveField(position, prop.content, ac);
                        else if (result is Gradient g)
                            EditorGUI.GradientField(position, prop.content, g);
                        else if (result is string str)
                            EditorGUI.TextField(position, prop.content, str);
                        else if (result is bool bl)
                            EditorGUI.Toggle(position, prop.content, bl);
                        else if (result is int i)
                            EditorGUI.IntField(position, prop.content, i);
                        else if (result is float f)
                            EditorGUI.FloatField(position, prop.content, f);
                        else if (result is double d)
                            EditorGUI.DoubleField(position, prop.content, d);
                        else if (result is long l)
                            EditorGUI.LongField(position, prop.content, l);
                        else if (result is ulong ul)
                            EditorGUI.LongField(position, prop.content, (long)ul);
                        else if (result is Enum e)
                            EditorGUI.EnumPopup(position, prop.content, e);
                        else
                            EditorGUI.LabelField(position, prop.content, Utils.GetTempContent(result.ToString()));
                    }
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

        VisibilityState TryReolveProperty(SerializedProperty property, bool resultRequired, out object result) {
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || EditorApplication.isPlayingOrWillChangePlaymode) {
                result = null;
                return property == null ? VisibilityState.Hidden : VisibilityState.Visible;
            }
            result = property.objectReferenceValue;
            var attr = attribute as ResolveAttribute;
            if (attr == null) return VisibilityState.Visible;
            var target = property.serializedObject.targetObject as UdonSharpBehaviour;
            if (target == null) return VisibilityState.Visible;
            var fieldInfo = Utils.GetFieldInfoFromProperty(property, out var _);
            if (result.IsValid() && attr.NullOnly) return VisibilityState.Visible;
            bool hide = attr.HideInInspectorIfResolvable;
            if (!hide && !resultRequired) return VisibilityState.Visible;
            return new ResolvePreprocessor().TryResolve(fieldInfo, target, out result) && result.IsValid() ? hide ?
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