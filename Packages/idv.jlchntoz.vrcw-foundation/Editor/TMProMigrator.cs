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
            using (PooledObjectExtensions.Get(out List<TextMeshProUGUI> temp))
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
            using (PooledObjectExtensions.Get(out Dictionary<Type, bool> migratableTypes))
            using (PooledObjectExtensions.Get(out Dictionary<Type, Dictionary<FieldInfo, FieldInfo>> migratableFields))
            using (PooledObjectExtensions.Get(out Dictionary<FieldInfo, TextMeshProUGUI> newCreated))
                foreach (var monoBehaviour in root.IterateAllComponents<MonoBehaviour>(true)) {
                    var type = monoBehaviour.GetType();
                    if (!migratableTypes.TryGetValue(type, out var isTypeMigratable))
                        migratableTypes[type] = isTypeMigratable = type.GetCustomAttribute<TMProMigratableAttribute>() != null;
                    if (!migratableFields.TryGetValue(type, out var mapping)) {
                        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                            var attr = field.GetCustomAttribute<TMProMigratableAttribute>();
                            if (attr == null) continue;
                            mapping ??= new Dictionary<FieldInfo, FieldInfo>();
                            var targetField = type.GetField(attr.TMProFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (targetField != null)
                                mapping[field] = targetField;
                        }
                        migratableFields[type] = mapping;
                    }
                    if (isTypeMigratable) {
                        if (monoBehaviour.TryGetComponent(out Text text) &&
                            ComponentReplacer.CanAllReferencesReplaceWith<TextMeshProUGUI>(text))
                            Migrate(text);
                        if (monoBehaviour.TryGetComponent(out TextMesh textMesh) &&
                            ComponentReplacer.CanAllReferencesReplaceWith<TextMesh>(textMesh))
                            Migrate(textMesh);
                    }
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
            using (PooledObjectExtensions.Get(out List<Component> components)) {
                foreach (var text in root.GetComponentsInChildren<Text>(true)) {
                    if (!ComponentReplacer.CanAllReferencesReplaceWith<TextMeshProUGUI>(text)) continue;
                    text.GetComponents(components);
                    bool isRequired = false;
                    foreach (var component in components)
                        if (component != text && DependencyUtils.IsRequired(component.GetType(), typeof(Text), typeof(TextMeshProUGUI))) {
                            isRequired = true;
                            break;
                        }
                    if (!isRequired) Migrate(text);
                }
                foreach (var text in root.GetComponentsInChildren<TextMesh>(true)) {
                    if (!ComponentReplacer.CanAllReferencesReplaceWith<TextMeshPro>(text)) continue;
                    text.GetComponents(components);
                    bool isRequired = false;
                    foreach (var component in components)
                        if (component != text && DependencyUtils.IsRequired(component.GetType(), typeof(TextMesh), typeof(TextMeshPro))) {
                            isRequired = true;
                            break;
                        }
                    if (!isRequired) Migrate(text);
                }
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
            tmpComponent.fontStyle = fontStyle switch {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal,
            };
            tmpComponent.alignment = alignment switch {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => alignByGeometry ? TextAlignmentOptions.TopGeoAligned : TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => alignByGeometry ? TextAlignmentOptions.CenterGeoAligned : TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => alignByGeometry ? TextAlignmentOptions.BottomGeoAligned : TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => alignByGeometry ? TextAlignmentOptions.MidlineGeoAligned : TextAlignmentOptions.Center,
            };
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

        public static TextMeshPro Migrate(TextMesh textMesh) {
            if (textMesh == null) return null;
            var text = textMesh.text;
            var font = textMesh.font;
            var fontSize = textMesh.fontSize;
            var fontStyle = textMesh.fontStyle;
            var anchor = textMesh.anchor;
            var align = textMesh.alignment;
            var color = textMesh.color;
            var lineSpacing = textMesh.lineSpacing;
            var richText = textMesh.richText;
            var tmpComponent = ComponentReplacer.TryReplaceComponent<TextMeshPro>(textMesh, false);
            if (tmpComponent == null) return null;
            tmpComponent.text = text;
            if (font == null || !fontAssetMapping.TryGetValue(font, out var fontAsset)) {
                fontAsset = TMP_Settings.GetFontAsset();
                if (font != null && fontAsset != null) fontAssetMapping[font] = fontAsset;
            }
            tmpComponent.font = fontAsset;
            tmpComponent.fontSize = fontSize > 0 ? fontSize : font != null ? font.fontSize : 0;
            tmpComponent.fontStyle = fontStyle switch {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal,
            };
            var alignment = (TextAlignmentOptions)(align switch {
                TextAlignment.Left => HorizontalAlignmentOptions.Left,
                TextAlignment.Right => HorizontalAlignmentOptions.Right,
                _ => HorizontalAlignmentOptions.Center,
            });
            tmpComponent.enableWordWrapping = false;
            tmpComponent.lineSpacing = (lineSpacing - 1) * 100;
            tmpComponent.richText = richText;
            if (tmpComponent.TryGetComponent(out RectTransform rectTransform)) {
                var pos = anchor switch {
                    TextAnchor.UpperLeft => new Vector2(0, 1),
                    TextAnchor.UpperCenter => new Vector2(0.5f, 1),
                    TextAnchor.UpperRight => new Vector2(1, 1),
                    TextAnchor.MiddleLeft => new Vector2(0, 0.5f),
                    TextAnchor.MiddleRight => new Vector2(1, 0.5f),
                    TextAnchor.LowerLeft => new Vector2(0, 0),
                    TextAnchor.LowerCenter => new Vector2(0.5f, 0),
                    TextAnchor.LowerRight => new Vector2(1, 0),
                    _ => new Vector2(0.5f, 0.5f),
                };
                alignment |= (TextAlignmentOptions)(anchor switch {
                    TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => VerticalAlignmentOptions.Top,
                    TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => VerticalAlignmentOptions.Bottom,
                    _ => VerticalAlignmentOptions.Middle,
                });
                rectTransform.pivot = pos;
                rectTransform.anchorMin = pos;
                rectTransform.anchorMax = pos;
                if (tmpComponent.isActiveAndEnabled) {
                    tmpComponent.ForceMeshUpdate();
                    rectTransform.sizeDelta = tmpComponent.GetRenderedValues(false);
                } else {
                    var temp = new GameObject("TempObject", typeof(RectTransform), typeof(TextMeshPro));
                    if (temp.TryGetComponent(out TextMeshPro tempTMP)) {
                        EditorUtility.CopySerialized(tmpComponent, tempTMP);
                        tempTMP.ForceMeshUpdate();
                        rectTransform.sizeDelta = tempTMP.GetRenderedValues(false);
                    }
                    UnityObject.DestroyImmediate(temp);
                }
                tmpComponent.enableWordWrapping = true;
            } else
                alignment |= (TextAlignmentOptions)VerticalAlignmentOptions.Middle;
            tmpComponent.alignment = alignment;
            tmpComponent.color = color;
            return tmpComponent;
        }
    }
}