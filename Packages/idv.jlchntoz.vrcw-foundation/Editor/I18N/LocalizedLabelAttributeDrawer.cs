using UnityEngine;
using UnityEditor;
using JLChnToZ.VRC.Foundation.Editors;
using System.Collections.Generic;
using System;

namespace JLChnToZ.VRC.Foundation.I18N.Editors {
    [CustomPropertyDrawer(typeof(LocalizedLabelAttribute))]
    public class LocalizedLabelAttributeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Resolve(property, label);
            EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            Resolve(property, label);
            return EditorGUI.GetPropertyHeight(property, label);
        }

        void Resolve(SerializedProperty property, GUIContent label) =>
            Resolve(attribute as LocalizedLabelAttribute, property, label);

        public static GUIContent Resolve(SerializedProperty property) {
            var label = Utils.GetTempContent(property.displayName, property.tooltip);
            Resolve(null, property, label);
            return label;
        }

        public static void Resolve(LocalizedLabelAttribute attr, SerializedProperty property, GUIContent label) {
            var i18n = EditorI18N.Instance;
            var key = attr?.Key;
            var field = Utils.GetFieldInfoFromProperty(property, out var _);
            if (string.IsNullOrEmpty(key)) key = $"{field.DeclaringType}.{property.propertyPath}";
            var value = i18n[key];
            if (!string.IsNullOrEmpty(value)) label.text = value;
            var tooltipKey = attr?.TooltipKey;
            if (string.IsNullOrEmpty(tooltipKey)) tooltipKey = $"{key}:tooltip";
            value = i18n[tooltipKey];
            if (!string.IsNullOrEmpty(value)) label.tooltip = value;
        }
    }

    [CustomPropertyDrawer(typeof(LocalizedHeaderAttribute))]
    public class LocalizedHeaderAttributeDrawer : DecoratorDrawer {
        public override void OnGUI(Rect position) {
            var attr = attribute as LocalizedHeaderAttribute;
            var i18n = EditorI18N.Instance;
            var key = attr.Key;
            var value = i18n[key];
            if (string.IsNullOrEmpty(value)) value = $"<Unlocalized: {key}>";
            position.height = EditorGUIUtility.singleLineHeight;
            position.y += EditorGUIUtility.singleLineHeight;
            GUI.Label(position, value, EditorStyles.boldLabel);
        }

        public override float GetHeight() => EditorGUIUtility.singleLineHeight * 2;
    }

    [CustomPropertyDrawer(typeof(LocalizedEnumAttribute))]
    public class LocalizedEnumAttributeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Utils.GetFieldInfoFromProperty(property, out var type);
            if (type == null || !type.IsEnum) {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            var attr = attribute as LocalizedEnumAttribute;
            var enumData = EditorI18N.Instance.GetLocalizedEnum(type, attr?.Key);
            using (new EditorGUI.PropertyScope(position, label, property))
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                if (enumData.isFlags) {
                    long value = property.longValue;
                    int mask = 0;
                    for (int i = 0; i < enumData.enumValues.Length; i++)
                        if ((value & enumData.enumValues[i]) != 0)
                            mask |= 1 << i;
                    int newMask = EditorGUI.MaskField(position, label, mask, enumData.enumNames as string[]);
                    if (changeCheck.changed) {
                        int changes = newMask ^ mask;
                        if (changes != 0) {
                            for (int i = 0; i < enumData.enumValues.Length; i++) {
                                int bit = 1 << i;
                                if ((changes & bit) != 0) {
                                    if ((newMask & bit) != 0)
                                        value |= enumData.enumValues[i];
                                    else
                                        value &= ~enumData.enumValues[i];
                                }
                            }
                            property.longValue = value;
                        }
                    }
                } else {
                    int index = Mathf.Max(0, Array.IndexOf(enumData.enumValues, property.longValue));
                    index = EditorGUI.Popup(position, label, index, enumData.enumNames as GUIContent[]);
                    if (changeCheck.changed) property.longValue = enumData.enumValues[index];
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            Utils.GetFieldInfoFromProperty(property, out var type);
            return type == null || !type.IsEnum ?
                EditorGUI.GetPropertyHeight(property, label) :
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}