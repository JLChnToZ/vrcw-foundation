using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    [CustomPropertyDrawer(typeof(UdonMetaAttribute))]
    public class UdonMetaAttributeDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
    }
}