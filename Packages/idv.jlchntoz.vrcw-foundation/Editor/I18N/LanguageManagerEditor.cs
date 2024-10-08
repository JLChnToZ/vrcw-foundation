using System;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using JLChnToZ.VRC.Foundation.Editors;

namespace JLChnToZ.VRC.Foundation.I18N.Editors {
    [CustomEditor(typeof(LanguageManager))]
    public class LanguageManagerEditor : Editor {
        static GUIContent textContent;
        SerializedProperty languageJsonFiles, languageJson;
        LanguageEditorWindow openedWindow;
        SerializedReorderableList languageJsonFilesList;
        GUIStyle wrappedTextAreaStyle;
        bool showJson = false;
        [NonSerialized] bool hasInit;

        protected void OnEnable() {
            if (textContent == null) textContent = new GUIContent();
            if (wrappedTextAreaStyle == null)
                wrappedTextAreaStyle = new GUIStyle(EditorStyles.textArea) {
                    wordWrap = true
                };
            languageJsonFiles = serializedObject.FindProperty("languageJsonFiles");
            languageJson = serializedObject.FindProperty("languageJson");
            languageJsonFilesList = new SerializedReorderableList(languageJsonFiles);
            hasInit = true;
        }
        
        public override void OnInspectorGUI() {
            if (!hasInit) OnEnable();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, drawScript: false)) return;
            if (GUILayout.Button(EditorI18N.Instance.GetLocalizedContent("JLChnToZ.VRC.Foundation.I18N.LanguageManager.openLanguageEditor"))) {
                if (openedWindow != null) openedWindow.Focus();
                else openedWindow = LanguageEditorWindow.Open(target as LanguageManager);
            }
            EditorGUILayout.Space();
            serializedObject.Update();
            using (var changed = new EditorGUI.ChangeCheckScope()) {
                languageJsonFilesList.DoLayoutList();
                if (changed.changed && openedWindow != null) openedWindow.RefreshJsonLists();
            }
            if (openedWindow == null || openedWindow.LanguageManager != target) openedWindow = null;
            if (showJson = EditorGUILayout.Foldout(showJson, LocalizedLabelAttributeDrawer.Resolve(languageJson), true)) {
                textContent.text = languageJson.stringValue;
                var height = wrappedTextAreaStyle.CalcHeight(textContent, EditorGUIUtility.currentViewWidth);
                var rect = EditorGUILayout.GetControlRect(false, height);
                using (var propScope = new EditorGUI.PropertyScope(rect, textContent, languageJson))
                using (var changeScope = new EditorGUI.ChangeCheckScope()) {
                    var newJson = EditorGUI.TextArea(rect, languageJson.stringValue, wrappedTextAreaStyle);
                    if (changeScope.changed) languageJson.stringValue = newJson;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}