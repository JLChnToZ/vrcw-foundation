using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.I18N.Editors {
    using static JLChnToZ.VRC.Foundation.Editors.Utils;

    public static class I18NUtils {
        public static GUIContent GetLocalizedContent(this EditorI18N i18n, string key) =>
            GetTempContent(i18n.GetOrDefault(key), i18n[$"{key}:tooltip"]);

        public static GUIContent GetLocalizedContent(this EditorI18N i18n, string key, params object[] format) =>
            GetTempContent(string.Format(i18n.GetOrDefault(key), format), i18n[$"{key}:tooltip"]);
        
        public static void DisplayLocalizedDialog1(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialog(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive")
            );

        public static void DisplayLocalizedDialog1(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialog(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args)
            );

        public static bool DisplayLocalizedDialog2(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialog(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive"),
                i18n.GetOrDefault($"{key}:negative")
            );

        public static bool DisplayLocalizedDialog2(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialog(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args),
                string.Format(i18n.GetOrDefault($"{key}:negative"), args)
            );

        public static int DisplayLocalizedDialog3(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialogComplex(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive"),
                i18n.GetOrDefault($"{key}:negative"),
                i18n.GetOrDefault($"{key}:alt")
            );
        
        public static int DisplayLocalizedDialog3(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialogComplex(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args),
                string.Format(i18n.GetOrDefault($"{key}:negative"), args),
                string.Format(i18n.GetOrDefault($"{key}:alt"), args)
            );

        public static void DrawLocaleField() {
            var i18n = EditorI18N.Instance;
            var languageIndex = i18n.LanguageIndex;
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                languageIndex = EditorGUILayout.Popup(i18n.GetOrDefault("Language"), languageIndex, i18n.LanguageNames);
                if (changeCheck.changed) i18n.LanguageIndex = languageIndex;
            }
            var machineTranslated = i18n["MachineTranslationMessage"];
            if (!string.IsNullOrEmpty(machineTranslated)) EditorGUILayout.HelpBox(machineTranslated, MessageType.Info);
        }
    }
}