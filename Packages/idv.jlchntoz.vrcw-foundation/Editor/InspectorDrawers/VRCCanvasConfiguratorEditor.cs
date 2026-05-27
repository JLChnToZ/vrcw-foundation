using UnityEngine;
using UnityEditor;
using VRC.SDK3.Components;
using UnityEngine.UI;
using JLChnToZ.VRC.Foundation.I18N;
using JLChnToZ.VRC.Foundation.I18N.Editors;

namespace JLChnToZ.VRC.Foundation.Editors {
    [CustomEditor(typeof(VRCCanvasConfigurator), true)]
    public class VRCCanvasConfiguratorEditor : Editor {
        static readonly GUILayoutOption[] buttonOptions = new[] { GUILayout.ExpandWidth(false) };
        SerializedProperty fixedAxisProp;
        SerializedProperty fixColliderProp;
        BoxCollider collider;
        Canvas canvas;
        GraphicRaycaster raycaster;
        VRCUiShape uiShape;
        EditorI18N locale;

        void OnEnable() {
            fixedAxisProp = serializedObject.FindProperty("fixedAxis");
            fixColliderProp = serializedObject.FindProperty("fixCollider");
            locale = EditorI18N.Instance;
        }

        public override void OnInspectorGUI() {
            I18NUtils.DrawLocaleField();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(locale["VRCCanvasConfigurator:message"], MessageType.None);
            EditorGUILayout.Space();
            var target = this.target as VRCCanvasConfigurator;
            var gameObject = target.gameObject;
            serializedObject.Update();
            if (canvas == null && !target.TryGetComponent(out canvas)) {
                if (ShowFixerGUI("VRCCanvasConfigurator.AddCanvas")) {
                    canvas = Undo.AddComponent<Canvas>(gameObject);
                    canvas.renderMode = RenderMode.WorldSpace;
                }
            } else {
                if (canvas.renderMode != RenderMode.WorldSpace) {
                    if (ShowFixerGUI("VRCCanvasConfigurator.SetRenderModeToWorldSpace")) {
                        Undo.RecordObject(canvas, "Set Canvas Render Mode");
                        canvas.renderMode = RenderMode.WorldSpace;
                        if (PrefabUtility.IsPartOfPrefabInstance(canvas))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
                    }
                }
            }
            if (raycaster == null && !target.TryGetComponent(out raycaster)) {
                if (ShowFixerGUI("VRCCanvasConfigurator.AddGraphicRaycaster"))
                    raycaster = Undo.AddComponent<GraphicRaycaster>(gameObject);
            }
            if (uiShape == null && !target.TryGetComponent(out uiShape)) {
                if (ShowFixerGUI("VRCCanvasConfigurator.AddVRCUiShape"))
                    uiShape = Undo.AddComponent<VRCUiShape>(gameObject);
            }
            if (gameObject.layer != 0) {
                if (ShowFixerGUI("VRCCanvasConfigurator.MoveToDefaultLayer")) {
                    Undo.RecordObject(gameObject, "Move to Default Layer");
                    gameObject.layer = 0;
                    if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
                }
            }
            if (collider == null && !target.TryGetComponent(out collider)) {
                if (ShowFixerGUI("VRCCanvasConfigurator.AddBoxCollider")) {
                    collider = Undo.AddComponent<BoxCollider>(gameObject);
                    fixColliderProp.boolValue = true;
                }
            } else
                EditorGUILayout.PropertyField(fixColliderProp);
            EditorGUILayout.PropertyField(fixedAxisProp);
            serializedObject.ApplyModifiedProperties();
        }

        bool ShowFixerGUI(string languageKey) {
            bool result;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(locale.GetLocalizedContent($"{languageKey}:message"), EditorStyles.wordWrappedLabel);
                result = GUILayout.Button(locale.GetLocalizedContent($"{languageKey}:confirm"), buttonOptions);
            }
            EditorGUILayout.Space();
            return result;
        }
    }
}