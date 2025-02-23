using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    public class QuaternionDrawer : MaterialPropertyDrawer {
        public QuaternionDrawer() { }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor) {
            if (prop.type != MaterialProperty.PropType.Vector) {
                editor.DefaultShaderProperty(prop, label);
                return;
            }
            MaterialEditor.BeginProperty(position, prop);
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                var value = prop.vectorValue;
                var euler = new Quaternion(value.x, value.y, value.z, value.w).eulerAngles;
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                euler = EditorGUI.Vector3Field(position, label, euler);
                EditorGUIUtility.labelWidth = labelWidth;
                if (changeCheck.changed) {
                    var q = Quaternion.Euler(euler);
                    prop.vectorValue = new Vector4(q.x, q.y, q.z, q.w);
                }
            }
            MaterialEditor.EndProperty();
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) =>
            MaterialEditor.GetDefaultPropertyHeight(prop);
    }
}
