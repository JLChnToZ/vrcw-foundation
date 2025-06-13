using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class Utils {
        static readonly StringBuilder sb = new StringBuilder();
        static readonly Stack<Transform> transformStack = new Stack<Transform>();
        static GUIContent tempContent;
        static readonly GetFieldInfoAndStaticTypeFromPropertyDelegate getFieldInfoAndStaticTypeFromProperty = Delegate.CreateDelegate(
            typeof(GetFieldInfoAndStaticTypeFromPropertyDelegate), Type
            .GetType("UnityEditor.ScriptAttributeUtility, UnityEditor", false)?
            .GetMethod("GetFieldInfoAndStaticTypeFromProperty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ) as GetFieldInfoAndStaticTypeFromPropertyDelegate;

        delegate FieldInfo GetFieldInfoAndStaticTypeFromPropertyDelegate(SerializedProperty property, out Type type);

        public static IEnumerable<T> IterateAllComponents<T>(this Scene scene, bool includeEditorOnly = false) where T : Component {
            var pending = new Stack<Transform>();
            var rootGameObjects = scene.GetRootGameObjects();
            for (int i = rootGameObjects.Length - 1; i >= 0; i--) pending.Push(rootGameObjects[i].transform);
            return IterateAllComponents<T>(pending, includeEditorOnly);
        }

        public static IEnumerable<T> IterateAllComponents<T>(this GameObject gameObject, bool includeEditorOnly = false) where T : Component {
            var pending = new Stack<Transform>();
            pending.Push(gameObject.transform);
            return IterateAllComponents<T>(pending, includeEditorOnly);
        }

        public static IEnumerable<T> IterateAllComponents<T>(Stack<Transform> pending, bool includeEditorOnly) where T : Component {
            var components = new List<T>();
            while (pending.TryPop(out var transform)) {
                if (transform == null || (!includeEditorOnly && transform.CompareTag("EditorOnly"))) continue;
                for (int i = transform.childCount - 1; i >= 0; i--) pending.Push(transform.GetChild(i));
                components.Clear();
                transform.GetComponents(components);
                foreach (var component in components) if (component != null) yield return component;
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

        public static void DeleteElement(SerializedProperty property, int index) {
            int size = property.arraySize;
            property.DeleteArrayElementAtIndex(index);
            if (size == property.arraySize) property.DeleteArrayElementAtIndex(index);
        }

        public static void DeleteElement<T>(ref T[] array, int index) {
            if (index < 0 || index >= array.Length) return;
            var newArray = new T[array.Length - 1];
            Array.Copy(array, 0, newArray, 0, index);
            if (index < array.Length - 1) Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);
            array = newArray;
        }

        public static TDelegate ToDelegate<TDelegate>(this MethodInfo method, object target = null) where TDelegate : Delegate =>
            (TDelegate)(method.IsStatic ?
                Delegate.CreateDelegate(typeof(TDelegate), method, false) :
                Delegate.CreateDelegate(typeof(TDelegate), target, method, false)
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GUIContent GetTempContent(SerializedProperty property) =>
            GetTempContent(property.displayName, property.tooltip);

        public static GUIContent GetTempContent(string text = "", string tooltip = "", Texture2D image = null) {
            if (tempContent == null) tempContent = new GUIContent();
            tempContent.text = text;
            tempContent.tooltip = tooltip;
            tempContent.image = image;
            return tempContent;
        }

        public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type) {
            if (getFieldInfoAndStaticTypeFromProperty == null) {
                type = null;
                return null;
            }
            return getFieldInfoAndStaticTypeFromProperty(property, out type);
        }

        public static bool IsValid(this object obj) {
            if (obj == null) return false;
            if (obj is UnityObject unityObj) return unityObj;
            return true;
        }

        public static void SetBoxedValue(this SerializedProperty serializedProperty, object value) {
#if UNITY_2022_1_OR_NEWER
            serializedProperty.boxedValue = value;
#else
            switch (serializedProperty.propertyType) {
                case SerializedPropertyType.Character:
                    serializedProperty.intValue = Convert.ToUInt16(value);
                    break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.ArraySize:
                    serializedProperty.longValue = Convert.ToInt64(value);
                    break;
                case SerializedPropertyType.Boolean:
                    serializedProperty.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.Float:
                    serializedProperty.doubleValue = Convert.ToDouble(value);
                    break;
                case SerializedPropertyType.String:
                    serializedProperty.stringValue = Convert.ToString(value);
                    break;
                case SerializedPropertyType.Color:
                    serializedProperty.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.ObjectReference:
                    serializedProperty.objectReferenceValue = (UnityObject)value;
                    break;
                case SerializedPropertyType.ExposedReference:
                    serializedProperty.exposedReferenceValue = (UnityObject)value;
                    break;
                case SerializedPropertyType.ManagedReference:
                    serializedProperty.managedReferenceValue = value;
                    break;
                case SerializedPropertyType.LayerMask:
                    serializedProperty.intValue = (int)value;
                    break;
                case SerializedPropertyType.Vector2:
                    serializedProperty.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    serializedProperty.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    serializedProperty.vector4Value = (Vector4)value;
                    break;
                case SerializedPropertyType.Rect:
                    serializedProperty.rectValue = (Rect)value;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    serializedProperty.animationCurveValue = (AnimationCurve)value;
                    break;
                case SerializedPropertyType.Bounds:
                    serializedProperty.boundsValue = (Bounds)value;
                    break;
                case SerializedPropertyType.Gradient:
                    serializedProperty.gradientValue = (Gradient)value;
                    break;
                case SerializedPropertyType.Quaternion:
                    serializedProperty.quaternionValue = (Quaternion)value;
                    break;
                case SerializedPropertyType.Vector2Int:
                    serializedProperty.vector2IntValue = (Vector2Int)value;
                    break;
                case SerializedPropertyType.Vector3Int:
                    serializedProperty.vector3IntValue = (Vector3Int)value;
                    break;
                case SerializedPropertyType.RectInt:
                    serializedProperty.rectIntValue = (RectInt)value;
                    break;
                case SerializedPropertyType.BoundsInt:
                    serializedProperty.boundsIntValue = (BoundsInt)value;
                    break;
#if UNITY_2021_1_OR_NEWER
                case SerializedPropertyType.Hash128:
                    serializedProperty.hash128Value = (Hash128)value;
                    break;
#endif
            }
#endif
        }

        public static string GetPath(this UnityObject obj) {
            if (obj == null) return "";
            if (obj is Transform transform) return GetPathFromHierarchy(transform);
            if (obj is Component component) return GetPathFromHierarchy(component.transform);
            if (obj is GameObject gameObject) return GetPathFromHierarchy(gameObject.transform);
            var assetPath = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(assetPath) ? obj.name : assetPath;
        }

        static string GetPathFromHierarchy(Transform transform) {
            try {
                for (var t = transform; t != null; t = t.parent)
                    transformStack.Push(t);
                while (transformStack.TryPop(out var t)) {
                    sb.Append(t.name);
                    if (transformStack.Count > 0) sb.Append('/');
                }
                return sb.ToString();
            } finally {
                sb.Clear();
                transformStack.Clear();
            }
        }

        public static bool GetTypedNamesAndValues(Type type, out string[] names, out long[] values, bool resolveDisplayName = true) {
            if (type == null || !type.IsEnum) {
                names = null;
                values = null;
                return false;
            }
            bool isFlags = type.GetCustomAttribute<FlagsAttribute>() != null;
            var rawValues = Enum.GetValues(type);
            var typeCode = Type.GetTypeCode(type);
            var nameList = new List<string>(rawValues.Length);
            var valueList = new List<long>(rawValues.Length);
            var valueSet = new HashSet<long>(rawValues.Length);
            for (int i = 0; i < rawValues.Length; i++) {
                var enumValue = rawValues.GetValue(i);
                long value = typeCode == TypeCode.UInt64 ?
                    unchecked((long)Convert.ToUInt64(enumValue)) :
                    Convert.ToInt64(enumValue);
                if (isFlags && value == 0) continue;
                var name = Enum.GetName(type, enumValue);
                var matchingMember = type.GetField(name);
                if (matchingMember != null) {
                    if (matchingMember.GetCustomAttribute<ObsoleteAttribute>() != null ||
                        matchingMember.GetCustomAttribute<HideInInspector>() != null) continue;
                    if (resolveDisplayName) {
                        var displayName = matchingMember.GetCustomAttribute<InspectorNameAttribute>();
                        if (displayName != null && !string.IsNullOrWhiteSpace(displayName.displayName))
                            name = displayName.displayName;
                        else
                            name = ObjectNames.NicifyVariableName(name);
                    }
                } else if (resolveDisplayName)
                    name = ObjectNames.NicifyVariableName(name);
                if (!valueSet.Add(value)) continue;
                nameList.Add(name);
                valueList.Add(value);
            }
            // If flag count still more than 32, sacrifice some of them,
            // starting from the ones with the most bits set,
            // which likely to be shorthands of common combinations,
            // including something like "All".
            if (isFlags && nameList.Count > 32) {
                int count = nameList.Count;
                var skip = new bool[count];
                var bitCount = new int[count];
                var bcSet = new HashSet<int>(count);
                // Calculate bit count for each value
                for (int i = 0; i < count; i++) {
                    long value = valueList[i];
                    value = (value & 0x5555555555555555L) + ((value >>  1) & 0x5555555555555555L);
                    value = (value & 0x3333333333333333L) + ((value >>  2) & 0x3333333333333333L);
                    value = (value & 0x0F0F0F0F0F0F0F0FL) + ((value >>  4) & 0x0F0F0F0F0F0F0F0FL);
                    value = (value & 0x00FF00FF00FF00FFL) + ((value >>  8) & 0x00FF00FF00FF00FFL);
                    value = (value & 0x0000FFFF0000FFFFL) + ((value >> 16) & 0x0000FFFF0000FFFFL);
                    value = (value & 0x00000000FFFFFFFFL) + ((value >> 32) & 0x00000000FFFFFFFFL);
                    bitCount[i] = (int)value;
                    bcSet.Add(i);
                }
                var bc = new int[bcSet.Count];
                bcSet.CopyTo(bc);
                Array.Sort(bc);
                for (int bci = bc.Length - 1, bcc = bitCount.Length; count > 32 && bci >= 0;) {
                    int i = Array.LastIndexOf(bitCount, bc[bci], bcc - 1);
                    if (i < 0) {
                        bci--;
                        bcc = bitCount.Length;
                    } else {
                        skip[i] = true;
                        count--;
                        bcc = i;
                    }
                }
                for (int i = skip.Length - 1; i >= 0; i--)
                    if (skip[i]) {
                        nameList.RemoveAt(i);
                        valueList.RemoveAt(i);
                    }
            }
            names = nameList.ToArray();
            values = valueList.ToArray();
            return true;
        }
    }
}