using UnityEngine;
using UnityEditor;

namespace JLChnToZ.VRC.Foundation.I18N.Editors {
    using static JLChnToZ.VRC.Foundation.Editors.Utils;

    /// <summary>
    /// Editor utilities for I18N.
    /// For drawing localized content, dialogs, and language selection field.
    /// </summary>
    public static class I18NUtils {
        /// <inheritdoc cref="GetLocalizedContent(EditorI18N, string, object[])"/>
        public static GUIContent GetLocalizedContent(this EditorI18N i18n, string key) =>
            GetTempContent(i18n.GetOrDefault(key), i18n[$"{key}:tooltip"]);

        /// <summary>
        /// Get localized content.
        /// </summary>
        /// <param name="i18n">The I18N instance.</param>
        /// <param name="key">The key of the content.</param>
        /// <param name="format">The format arguments.</param>
        /// <returns>The localized content.</returns>
        /// <remarks>
        /// The content should have two keys:
        /// <list type="bullet">
        /// <item><term>{key}</term><description>The content of the key.</description></item>
        /// <item><term>{key}:tooltip</term><description>Optional, the tooltip of the content.</description></item>
        /// </list>
        /// </remarks>
        public static GUIContent GetLocalizedContent(this EditorI18N i18n, string key, params object[] format) =>
            GetTempContent(string.Format(i18n.GetOrDefault(key), format), i18n[$"{key}:tooltip"]);
        

        /// <inheritdoc cref="DisplayLocalizedDialog1(EditorI18N, string, object[])"/>
        public static void DisplayLocalizedDialog1(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialog(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive")
            );

        /// <summary>
        /// Get localized dialog with one button.
        /// </summary>
        /// <param name="i18n">The I18N instance.</param>
        /// <param name="key">The key of the dialog.</param>
        /// <param name="args">The format arguments.</param>
        /// <remarks>
        /// The dialog should have three keys:
        /// <list type="bullet">
        /// <item><term>{key}:title</term><description>The title of the dialog.</description></item>
        /// <item><term>{key}:content</term><description>The content of the dialog.</description></item>
        /// <item><term>{key}:positive</term><description>The positive button text.</description></item>
        /// </list>
        /// </remarks>
        public static void DisplayLocalizedDialog1(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialog(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args)
            );

        /// <inheritdoc cref="DisplayLocalizedDialog2(EditorI18N, string, object[])"/>
        public static bool DisplayLocalizedDialog2(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialog(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive"),
                i18n.GetOrDefault($"{key}:negative")
            );

        /// <summary>
        /// Get localized dialog with two buttons.
        /// </summary>
        /// <param name="i18n">The I18N instance.</param>
        /// <param name="key">The key of the dialog.</param>
        /// <returns>The result of the dialog.</returns>
        /// <remarks>
        /// The dialog should have four keys:
        /// <list type="bullet">
        /// <item><term>{key}:title</term><description>The title of the dialog.</description></item>
        /// <item><term>{key}:content</term><description>The content of the dialog.</description></item>
        /// <item><term>{key}:positive</term><description>The positive button text, which returns <c>true</c>.</description></item>
        /// <item><term>{key}:negative</term><description>The negative button text, which returns <c>false</c>.</description></item>
        /// </list>
        /// </remarks>
        public static bool DisplayLocalizedDialog2(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialog(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args),
                string.Format(i18n.GetOrDefault($"{key}:negative"), args)
            );

        /// <inheritdoc cref="DisplayLocalizedDialog3(EditorI18N, string, object[])"/>
        public static int DisplayLocalizedDialog3(this EditorI18N i18n, string key) =>
            EditorUtility.DisplayDialogComplex(
                i18n.GetOrDefault($"{key}:title"),
                i18n.GetOrDefault($"{key}:content"),
                i18n.GetOrDefault($"{key}:positive"),
                i18n.GetOrDefault($"{key}:negative"),
                i18n.GetOrDefault($"{key}:alt")
            );
        
        /// <summary>
        /// Get localized dialog with three buttons.
        /// </summary>
        /// <param name="i18n">The I18N instance.</param>
        /// <param name="key">The key of the dialog.</param>
        /// <returns>The result of the dialog.</returns>
        /// <remarks>
        /// The dialog should have five keys:
        /// <list type="bullet">
        /// <item><term>{key}:title</term><description>The title of the dialog.</description></item>
        /// <item><term>{key}:content</term><description>The content of the dialog.</description></item>
        /// <item><term>{key}:positive</term><description>The positive button text, which returns <c>0</c>.</description></item>
        /// <item><term>{key}:negative</term><description>The negative button text, which returns <c>1</c>.</description></item>
        /// <item><term>{key}:alt</term><description>The alternative button text, which returns <c>2</c>.</description></item>
        /// </list>
        /// </remarks>
        public static int DisplayLocalizedDialog3(this EditorI18N i18n, string key, params object[] args) =>
            EditorUtility.DisplayDialogComplex(
                string.Format(i18n.GetOrDefault($"{key}:title"), args),
                string.Format(i18n.GetOrDefault($"{key}:content"), args),
                string.Format(i18n.GetOrDefault($"{key}:positive"), args),
                string.Format(i18n.GetOrDefault($"{key}:negative"), args),
                string.Format(i18n.GetOrDefault($"{key}:alt"), args)
            );

        /// <summary>
        /// Draw a language selection field.
        /// </summary>
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