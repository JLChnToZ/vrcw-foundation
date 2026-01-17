using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEditor;
using UdonSharpEditor;
using JLChnToZ.VRC.Foundation.Editors;

using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.I18N.Editors {
    [CustomEditor(typeof(LanguageManager))]
    public class LanguageManagerEditor : Editor {
        static GUIContent textContent;
        SerializedProperty languageJsonFiles, languageJson;
        LanguageEditorWindow openedWindow;
        SerializedReorderableList languageJsonFilesList;
        SerializedObject savedLanguageKeyObject;
        SerializedProperty savedLanguageKeyProperty;
        GUIStyle wrappedTextAreaStyle;
        bool showJson = false;
        [NonSerialized] bool hasInit;

        bool InitSavedLanguageKey() {
            var currentList = FindObjectsOfType<LanguageManager>(true);
            if (savedLanguageKeyObject != null) {
                var targetList = savedLanguageKeyObject.targetObjects;
                if (targetList.Length == currentList.Length)
                using (HashSetPool<UnityObject>.Get(out var currentSet)) {
                    currentSet.UnionWith(savedLanguageKeyObject.targetObjects);
                    var allMatch = true;
                    foreach (var lm in currentList) {
                        if (!currentSet.Contains(lm)) {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch) return false;
                }
                savedLanguageKeyObject.Dispose();
            }
            savedLanguageKeyObject = new SerializedObject(currentList);
            savedLanguageKeyProperty = savedLanguageKeyObject.FindProperty("savedLanguageKey");
            return true;
        }

        protected void OnEnable() {
            if (textContent == null) textContent = new GUIContent();
            if (wrappedTextAreaStyle == null)
                wrappedTextAreaStyle = new GUIStyle(EditorStyles.textArea) {
                    wordWrap = true
                };
            languageJsonFiles = serializedObject.FindProperty("languageJsonFiles");
            languageJson = serializedObject.FindProperty("languageJson");
            languageJsonFilesList = new SerializedReorderableList(languageJsonFiles);
            InitSavedLanguageKey();
            hasInit = true;
        }

        protected void OnDisable() {
            if (savedLanguageKeyObject != null) {
                savedLanguageKeyObject.Dispose();
                savedLanguageKeyObject = null;
                savedLanguageKeyProperty = null;
            }
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
            savedLanguageKeyObject.Update();
            EditorGUILayout.Space();
            using (var changed = new EditorGUI.ChangeCheckScope()) {
                EditorGUILayout.PropertyField(savedLanguageKeyProperty, true);
                if (changed.changed && !savedLanguageKeyProperty.hasMultipleDifferentValues) {
                    var value = savedLanguageKeyProperty.stringValue;
                    if (InitSavedLanguageKey()) {
                        savedLanguageKeyObject.Update();
                        savedLanguageKeyProperty.stringValue = value;
                    }
                }
            }
            savedLanguageKeyObject.ApplyModifiedProperties();
        }
    }
}