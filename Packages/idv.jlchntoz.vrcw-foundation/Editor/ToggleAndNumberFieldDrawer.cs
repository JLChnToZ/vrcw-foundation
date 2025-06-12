using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    [CustomPropertyDrawer(typeof(ToggleAndNumberFieldAttribute))]
    public class ToggleAndNumberFieldDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            using (new EditorGUI.PropertyScope(position, label, property))
            using (var changed = new EditorGUI.ChangeCheckScope()) {
                int intValue = property.intValue;
                bool toggleValue = intValue >= 0;
                var labelRect = position;
                labelRect.width = EditorGUIUtility.labelWidth;
                toggleValue = EditorGUI.ToggleLeft(labelRect, label, toggleValue);
                var rect = position;
                rect.xMin = labelRect.xMax + 2;
                if (toggleValue) {
                    intValue = EditorGUI.IntField(rect, intValue);
                    intValue = Mathf.Max(0, intValue);
                } else {
                    using (new EditorGUI.DisabledScope(true)) EditorGUI.TextField(rect, (attribute as ToggleAndNumberFieldAttribute)?.DisabledText ?? "");
                    intValue = -1;
                }
                if (changed.changed) property.intValue = intValue;
            }
        }
    }
}