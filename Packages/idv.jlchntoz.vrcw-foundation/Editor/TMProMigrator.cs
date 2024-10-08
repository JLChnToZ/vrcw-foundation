using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Cysharp.Threading.Tasks;

using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class TMProMigratator {
        static readonly Dictionary<Font, TMP_FontAsset> fontAssetMapping = new Dictionary<Font, TMP_FontAsset>();

        static TMProMigratator() {
            #if VRC_SDK_VRCSDK3
            ComponentReplacer.AddToBlackList(typeof(InputField), "m_Placeholder");
            ComponentReplacer.AddToBlackList(typeof(TMP_InputField), "m_Placeholder");
            ComponentReplacer.AddToBlackList(typeof(global::VRC.SDK3.Components.VRCUrlInputField), "m_Placeholder");
            #endif
        }

        [MenuItem("Tools/JLChnToZ VRCW Foundation/Migrate TMPro Components")]
        static void MigrateSelected() {
            LoadFontMapping();
            ComponentReplacer.InitAllComponents();
            foreach (var gameObject in Selection.gameObjects)
                Migrate(gameObject);
        }

        [MenuItem("Tools/JLChnToZ VRCW Foundation/Fix TMPro Visibility")]
        static void FixVisibility() {
            var temp = new List<TextMeshProUGUI>();
            foreach (var gameObject in Selection.gameObjects) {
                gameObject.GetComponentsInChildren(true, temp);
                foreach (var text in temp) {
                    if (text.enableAutoSizing) continue;
                    Undo.RecordObject(text, "Fix TMPro Visibility");
                    text.enableAutoSizing = true;
                    text.fontSizeMin = Mathf.Max(text.fontSize - 5, 0);
                    text.fontSizeMax = text.fontSize;
                }
            }
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        public static void LoadFontMapping() {
            fontAssetMapping.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets", "Packages" })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null) continue;
                var font = fontAsset.sourceFontFile;
                if (font == null) {
                    using (var so = new SerializedObject(fontAsset))
                        font = so.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue as Font;
                    if (font == null) continue;
                }
                fontAssetMapping[font] = fontAsset;
            }
        }

        public static void Migrate(GameObject root) => MigrateAsync(root).Forget();

        public static async UniTask MigrateAsync(GameObject root) {
            var migratableTypes = new Dictionary<Type, bool>();
            var migratableFields = new Dictionary<Type, Dictionary<FieldInfo, FieldInfo>>();
            var newCreated = new Dictionary<FieldInfo, TextMeshProUGUI>();
            foreach (var monoBehaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) {
                var type = monoBehaviour.GetType();
                if (!migratableTypes.TryGetValue(type, out var isTypeMigratable))
                    migratableTypes[type] = isTypeMigratable = type.GetCustomAttribute<TMProMigratableAttribute>() != null;
                if (!migratableFields.TryGetValue(type, out var mapping)) {
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                        var attr = field.GetCustomAttribute<TMProMigratableAttribute>();
                        if (attr == null) continue;
                        if (mapping == null) mapping = new Dictionary<FieldInfo, FieldInfo>();
                        var targetField = type.GetField(attr.TMProFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (targetField != null)
                            mapping[field] = targetField;
                    }
                    migratableFields[type] = mapping;
                }
                if (isTypeMigratable &&
                    monoBehaviour.TryGetComponent(out Text text) &&
                    ComponentReplacer.CanAllReferencesReplaceWith<TextMeshProUGUI>(text))
                    Migrate(text);
                if (mapping != null) {
                    newCreated.Clear();
                    foreach (var kv in mapping) {
                        var sourceField = kv.Key;
                        var sourceText = sourceField.GetValue(monoBehaviour) as Text;
                        if (kv.Value.GetValue(monoBehaviour) as UnityObject != null) continue;
                        if (sourceText == null) {
                            using (var so = new SerializedObject(monoBehaviour)) {
                                var prop = so.FindProperty(sourceField.Name);
                                if (prop == null || !prop.prefabOverride) continue;
                                PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                            }
                            await UniTask.Yield(); // Revert property override is asynchronous so we have to wait for it to finish.
                            sourceText = sourceField.GetValue(monoBehaviour) as Text;
                            if (sourceText == null) continue;
                        }
                        var targetText = Migrate(sourceText);
                        if (targetText == null) continue;
                        newCreated[sourceField] = targetText;
                    }
                    if (newCreated.Count > 0)
                        using (var so = new SerializedObject(monoBehaviour)) {
                            foreach (var kv in newCreated) {
                                if (!mapping.TryGetValue(kv.Key, out var destField)) continue;
                                var prop = so.FindProperty(kv.Key.Name);
                                if (prop != null) prop.objectReferenceValue = null;
                                prop = so.FindProperty(destField.Name);
                                if (prop != null) prop.objectReferenceValue = kv.Value;
                            }
                            so.ApplyModifiedProperties();
                        }
                }
            }
            var components = new List<Component>();
            foreach (var text in root.GetComponentsInChildren<Text>(true)) {
                if (!ComponentReplacer.CanAllReferencesReplaceWith<TextMeshProUGUI>(text)) continue;
                text.GetComponents(components);
                bool isRequired = false;
                foreach (var component in components)
                    if (component != text && ComponentReplacer.IsRequired(component.GetType(), typeof(Text), typeof(TextMeshProUGUI))) {
                        isRequired = true;
                        break;
                    }
                if (!isRequired) Migrate(text);
            }
            FixVisibility();
        }

        public static TextMeshProUGUI Migrate(Text textComponent) {
            if (textComponent == null) return null;
            var text = textComponent.text;
            var font = textComponent.font;
            var fontSize = textComponent.fontSize;
            var fontStyle = textComponent.fontStyle;
            var alignment = textComponent.alignment;
            var alignByGeometry = textComponent.alignByGeometry;
            var color = textComponent.color;
            var lineSpacing = textComponent.lineSpacing;
            var richText = textComponent.supportRichText;
            var vertOverflow = textComponent.verticalOverflow;
            var horzOverflow = textComponent.horizontalOverflow;
            var bestFit = textComponent.resizeTextForBestFit;
            var minSize = textComponent.resizeTextMinSize;
            var maxSize = textComponent.resizeTextMaxSize;
            var raycastTarget = textComponent.raycastTarget;
            var tmpComponent = ComponentReplacer.TryReplaceComponent<TextMeshProUGUI>(textComponent, false);
            if (tmpComponent == null) return null;
            tmpComponent.text = text;
            if (font == null || !fontAssetMapping.TryGetValue(font, out var fontAsset)) {
                fontAsset = TMP_Settings.GetFontAsset();
                if (font != null && fontAsset != null) fontAssetMapping[font] = fontAsset;
            }
            tmpComponent.font = fontAsset;
            tmpComponent.fontSize = fontSize;
            switch (fontStyle) {
                case FontStyle.Bold: tmpComponent.fontStyle = FontStyles.Bold; break;
                case FontStyle.Italic: tmpComponent.fontStyle = FontStyles.Italic; break;
                case FontStyle.BoldAndItalic: tmpComponent.fontStyle = FontStyles.Bold | FontStyles.Italic; break;
                default: tmpComponent.fontStyle = FontStyles.Normal; break;
            }
            switch (alignment) {
                case TextAnchor.UpperLeft: tmpComponent.alignment = TextAlignmentOptions.TopLeft; break;
                case TextAnchor.UpperCenter: tmpComponent.alignment = alignByGeometry ? TextAlignmentOptions.TopGeoAligned : TextAlignmentOptions.Top; break;
                case TextAnchor.UpperRight: tmpComponent.alignment = TextAlignmentOptions.TopRight; break;
                case TextAnchor.MiddleLeft: tmpComponent.alignment = TextAlignmentOptions.Left; break;
                case TextAnchor.MiddleCenter: tmpComponent.alignment = alignByGeometry ? TextAlignmentOptions.CenterGeoAligned : TextAlignmentOptions.Center; break;
                case TextAnchor.MiddleRight: tmpComponent.alignment = TextAlignmentOptions.Right; break;
                case TextAnchor.LowerLeft: tmpComponent.alignment = TextAlignmentOptions.BottomLeft; break;
                case TextAnchor.LowerCenter: tmpComponent.alignment = alignByGeometry ? TextAlignmentOptions.BottomGeoAligned : TextAlignmentOptions.Bottom; break;
                case TextAnchor.LowerRight: tmpComponent.alignment = TextAlignmentOptions.BottomRight; break;
                default: tmpComponent.alignment = alignByGeometry ? TextAlignmentOptions.MidlineGeoAligned : TextAlignmentOptions.Center; break;
            }
            tmpComponent.color = color;
            tmpComponent.lineSpacing = (lineSpacing - 1) * 100;
            tmpComponent.richText = richText;
            tmpComponent.overflowMode = vertOverflow == VerticalWrapMode.Truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            tmpComponent.enableWordWrapping = horzOverflow == HorizontalWrapMode.Wrap;
            tmpComponent.enableAutoSizing = true;
            if (bestFit) {
                tmpComponent.fontSizeMin = minSize;
                tmpComponent.fontSizeMax = maxSize;
            } else {
                tmpComponent.fontSizeMin = Mathf.Max(fontSize - 5, 0);
                tmpComponent.fontSizeMax = fontSize;
            }
            tmpComponent.raycastTarget = raycastTarget;
            return tmpComponent;
        }
    }
}