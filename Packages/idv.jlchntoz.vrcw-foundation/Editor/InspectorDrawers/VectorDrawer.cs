using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    public class VectorDrawer : MaterialPropertyDrawer {
        static readonly float[] tempFloats1 = new float[1], tempFloats2 = new float[2], tempFloats3 = new float[3], tempFloats4 = new float[4];
        readonly int dimension;
        readonly GUIContent[] labels;

        protected VectorDrawer(int dimension) {
            this.dimension = dimension;
            if (this.dimension < 1 || this.dimension > 4)
                this.dimension = 4;
            labels = new GUIContent[this.dimension];
            if (this.dimension > 0) labels[0] = new GUIContent("X");
            if (this.dimension > 1) labels[1] = new GUIContent("Y");
            if (this.dimension > 2) labels[2] = new GUIContent("Z");
            if (this.dimension > 3) labels[3] = new GUIContent("W");
        }

        public VectorDrawer(string label1, string label2) {
            dimension = 2;
            labels = new[] {
                new GUIContent(label1),
                new GUIContent(label2),
            };
        }

        public VectorDrawer(string label1, string label2, string label3) {
            dimension = 3;
            labels = new[] {
                new GUIContent(label1),
                new GUIContent(label2),
                new GUIContent(label3),
            };
        }

        public VectorDrawer(string label1, string label2, string label3, string label4) {
            dimension = 4;
            labels = new[] {
                new GUIContent(label1),
                new GUIContent(label2),
                new GUIContent(label3),
                new GUIContent(label4),
            };
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) {
            if (prop.type != MaterialProperty.PropType.Vector)
                return MaterialEditor.GetDefaultPropertyHeight(prop);
            return EditorGUIUtility.singleLineHeight * 2;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor) {
            if (prop.type != MaterialProperty.PropType.Vector) {
                editor.DefaultShaderProperty(prop, label);
                return;
            }
            MaterialEditor.BeginProperty(position, prop);
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                var value = prop.vectorValue;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);
                position.y += position.height;
                position.xMin += 16;
                var floats = PrepareFloats(prop);
                EditorGUI.MultiFloatField(position, labels, floats);
                if (changeCheck.changed) WriteFloats(prop, floats);
            }
            MaterialEditor.EndProperty();
        }

        float[] PrepareFloats(MaterialProperty prop) {
            var v = prop.vectorValue;
            switch (dimension) {
                case 1:
                    tempFloats1[0] = v.x;
                    return tempFloats1;
                case 2:
                    tempFloats2[0] = v.x;
                    tempFloats2[1] = v.y;
                    return tempFloats2;
                case 3:
                    tempFloats3[0] = v.x;
                    tempFloats3[1] = v.y;
                    tempFloats3[2] = v.z;
                    return tempFloats3;
                case 4:
                    tempFloats4[0] = v.x;
                    tempFloats4[1] = v.y;
                    tempFloats4[2] = v.z;
                    tempFloats4[3] = v.w;
                    return tempFloats4;
                default:
                    Debug.LogError($"Invalid dimension {dimension} for VectorDrawer.");
                    return null;
            }
        }

        void WriteFloats(MaterialProperty prop, float[] floats) {
            var v = prop.vectorValue;
            if (dimension > 0) v.x = floats[0];
            if (dimension > 1) v.y = floats[1];
            if (dimension > 2) v.z = floats[2];
            if (dimension > 3) v.w = floats[3];
            prop.vectorValue = v;
        }
    }

    public class Vector2Drawer : VectorDrawer {
        public Vector2Drawer() : base(2) { }
        public Vector2Drawer(string label1, string label2) : base(label1, label2) { }
    }

    public class Vector3Drawer : VectorDrawer {
        public Vector3Drawer() : base(3) { }
        public Vector3Drawer(string label1, string label2, string label3) : base(label1, label2, label3) { }
    }

    public class Vector4Drawer : VectorDrawer {
        public Vector4Drawer() : base(4) { }
        public Vector4Drawer(string label1, string label2, string label3, string label4) : base(label1, label2, label3, label4) { }
    }
}