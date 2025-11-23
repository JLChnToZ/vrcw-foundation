using UnityEngine;
using UnityEditor;
using JLChnToZ.VRC.Foundation.I18N;
using JLChnToZ.VRC.Foundation.I18N.Editors;

namespace JLChnToZ.VRCW.Foundation.Editor {
    public class UIModifiedInspector : ShaderGUI {
        private EditorI18N locale;

        MaterialEditor editor;
        MaterialProperty[] properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props) {
            editor = materialEditor;
            properties = props;
            locale = EditorI18N.Instance;

            I18NUtils.DrawLocaleField();
            EditorGUILayout.Space();

            using var changed = new EditorGUI.ChangeCheckScope();

            DrawMainSettings();
            EditorGUILayout.Space();

            DrawSDFSettings();
            EditorGUILayout.Space();

            DrawVRCSettings();
            EditorGUILayout.Space();

            DrawExperimentalSettings();
            EditorGUILayout.Space();

            // Other Settings
            FindAndDrawProperty("_UseUIAlphaClip", Loc("UIModified.UseAlphaClip", "Use Alpha Clip"));
            FindAndDrawProperty("_Cull", Loc("UIModified.CullMode", "Cull Mode"));
            FindAndDrawProperty("_ColorMask", Loc("UIModified.ColorMask", "Color Mask"));
            FindAndDrawProperty("_StencilComp", Loc("UIModified.Stencil.Comp", "Comparison Mode"));
            FindAndDrawProperty("_Stencil", Loc("UIModified.Stencil.ID", "ID"));
            FindAndDrawProperty("_StencilOp", Loc("UIModified.Stencil.Operation", "Operation"));
            FindAndDrawProperty("_StencilWriteMask", Loc("UIModified.Stencil.WriteMask", "Write Mask"));
            FindAndDrawProperty("_StencilReadMask", Loc("UIModified.Stencil.ReadMask", "Read Mask"));
            FindAndDrawProperty("unity_GUIZTestMode", Loc("UIModified.ZTestMode", "Z Test Mode"));
            FindAndDrawProperty("_ZWrite", Loc("UIModified.ZWrite", "Z Write"));
            editor.RenderQueueField();
            editor.EnableInstancingField();

            if (changed.changed) {
                // Update keywords for all target materials
                foreach (var obj in editor.targets) {
                    var mat = obj as Material;
                    if (mat == null) continue;
                    UpdateKeywords(mat);
                }
            }
        }

        void DrawMainSettings() {
            var mainTex = FindProperty("_MainTex");
            var color = FindProperty("_Color") ?? FindProperty("_FaceColor");
            if (mainTex != null && color != null)
                editor.TexturePropertySingleLine(new GUIContent(Loc("UIModified.SpriteAndColor", "Sprite and Color")), mainTex, color);
            if (FindAndDrawProperty("_TextureWidth") |
                FindAndDrawProperty("_TextureHeight") |
                FindAndDrawProperty("_GradientScale") |
                FindAndDrawProperty("_WeightNormal") |
                FindAndDrawProperty("_WeightBold") |
                FindAndDrawProperty("_ScaleRatioA")) {
                EditorGUILayout.HelpBox(Loc("UIModified.Help.FontSettings", "These properties are controlled by TextMeshPro and should not be edited manually."), MessageType.Info);
            }
        }

        void DrawSDFSettings() {
            var useSdf = FindProperty("_UseSDF");
            var useMsdf = FindProperty("_UseMSDF");
            if (useSdf == null && useMsdf == null) return;
            EditorGUILayout.LabelField(Loc("UIModified.SDFSettings", "SDF Settings"), EditorStyles.boldLabel);
            int sdfMode = 0;
            if (IsPropertyOn(useSdf))
                sdfMode = 1;
            else if (IsPropertyOn(useMsdf))
                sdfMode = 2;
            string[] sdfModes = new[] {
                Loc("UIModified.SDFMode.NonSDF", "Non SDF"),
                Loc("UIModified.SDFMode.SDF", "SDF"),
                Loc("UIModified.SDFMode.MSDF", "MSDF")
            };
            int newSdfMode = EditorGUILayout.Popup(Loc("UIModified.TextureType", "Texture Type"), sdfMode, sdfModes);
            if (newSdfMode != sdfMode) {
                if (useSdf != null) useSdf.floatValue = (newSdfMode == 1) ? 1f : 0f;
                if (useMsdf != null) useMsdf.floatValue = (newSdfMode == 2) ? 1f : 0f;
            }
            if (sdfMode != 0) {
                var overrideMsdf = FindProperty("_OverrideMSDF");
                if (overrideMsdf != null) editor.ShaderProperty(overrideMsdf, Loc("UIModified.UseOverrideTexture", "Use Override Texture"));
                if (overrideMsdf != null && overrideMsdf.floatValue > 0.5f) {
                    var msdfTex = FindProperty("_MSDFTex");
                    if (msdfTex != null) editor.TexturePropertySingleLine(new GUIContent(Loc("UIModified.OverrideTexture", "Override Texture")), msdfTex);
                }
                FindAndDrawProperty("_PixelRange", Loc("UIModified.PixelRange", "Pixel Range"));
                FindAndDrawProperty("_SDFThreshold", Loc("UIModified.Threshold", "Threshold"));
            }
        }

        void DrawVRCSettings() {
            EditorGUILayout.LabelField(Loc("UIModified.VRChatSettings", "VRChat Settings"), EditorStyles.boldLabel);
            var vrcSupport = FindProperty("_VRCSupport");
            bool isOn = false;
            if (vrcSupport != null) {
                MaterialEditor.BeginProperty(vrcSupport);
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    isOn = IsPropertyOn(vrcSupport);
                    isOn = EditorGUILayout.ToggleLeft(Loc("UIModified.ConditionalAppearance", "Conditional Appearance in Handheld Camera/Mirror"), isOn);
                    if (check.changed) vrcSupport.floatValue = isOn ? 1f : 0f;
                }
                MaterialEditor.EndProperty();
            }
            if (isOn) FindAndDrawProperty("_RenderMode", Loc("UIModified.VisibleModes", "Visible Modes"));
            FindAndDrawProperty("_MirrorFlip", Loc("UIModified.FlipInMirror", "Flip in Mirror"));
        }

        void DrawExperimentalSettings() {
            EditorGUILayout.LabelField(Loc("UIModified.ExperimentalSettings", "Special Render Mode Settings (Experimental)"), EditorStyles.boldLabel);
            var doubleSided = FindProperty("_DoubleSided");
            var billboard = FindProperty("_Billboard");
            var screenSpace = FindProperty("_ScreenSpaceOverlay");
            int renderMode = 0;
            if (IsPropertyOn(doubleSided))
                renderMode = 1;
            else if (IsPropertyOn(billboard))
                renderMode = 2;
            else if (IsPropertyOn(screenSpace))
                renderMode = 3;
            string[] specialRenderModes = new[] {
                Loc("UIModified.RenderMode.Normal", "Normal"),
                Loc("UIModified.RenderMode.DoubleSided", "Double Sided"),
                Loc("UIModified.RenderMode.Billboard", "Billboard"),
                Loc("UIModified.RenderMode.ScreenSpaceOverlay", "Screen Space Overlay")
            };
            var newRenderMode = EditorGUILayout.Popup(Loc("UIModified.RenderMode", "Render Mode"), renderMode, specialRenderModes);
            if (newRenderMode != renderMode) {
                if (doubleSided != null) doubleSided.floatValue = (newRenderMode == 1) ? 1f : 0f;
                if (billboard != null) billboard.floatValue = (newRenderMode == 2) ? 1f : 0f;
                if (screenSpace != null) screenSpace.floatValue = (newRenderMode == 3) ? 1f : 0f;
            }
            switch (renderMode) {
                case 1:
                    EditorGUILayout.HelpBox(Loc("UIModified.Help.DoubleSided", "Double Sided mode requires geometry shader support. It don't work on mobile platforms."), MessageType.Info);
                    break;
                case 2:
                    EditorGUILayout.HelpBox(Loc("UIModified.Help.Billboard", "Billboard mode requires your canvas element has zero rotation in world space to work correctly."), MessageType.Info);
                    break;
                case 3: {
                        var canvasRect = FindProperty("_CanvasRect");
                        if (canvasRect != null) {
                            MaterialEditor.BeginProperty(canvasRect);
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var value = canvasRect.vectorValue;
                                var rectValue = new Rect(value.x, value.y, value.z, value.w);
                                rectValue = EditorGUILayout.RectField(Loc("UIModified.CanvasRect", "Canvas Rect"), rectValue);
                                if (check.changed) canvasRect.vectorValue = new Vector4(rectValue.x, rectValue.y, rectValue.width, rectValue.height);
                            }
                            MaterialEditor.EndProperty();
                        }
                        var aspect = FindProperty("_AspectRatioMatch");
                        if (aspect != null) {
                            MaterialEditor.BeginProperty(aspect);
                            EditorGUILayout.PrefixLabel(Loc("UIModified.AspectRatioMatch", "Aspect Ratio Match"));
                            using (new EditorGUILayout.HorizontalScope())
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var value = aspect.floatValue;
                                GUILayout.Label(Loc("UIModified.WidthLabel", "W"));
                                value = EditorGUILayout.Slider(value, 0f, 1f, GUILayout.ExpandWidth(true));
                                GUILayout.Label(Loc("UIModified.HeightLabel", "H"));
                                if (check.changed) aspect.floatValue = value;
                            }
                            MaterialEditor.EndProperty();
                        }
                    }
                    break;
            }
        }

        MaterialProperty FindProperty(string name) {
            try {
                return FindProperty(name, properties);
            } catch {
                return null;
            }
        }

        bool FindAndDrawProperty(string name, string displayName = null) {
            var prop = FindProperty(name);
            if (prop != null) {
                editor.ShaderProperty(prop, string.IsNullOrEmpty(displayName) ? prop.displayName : displayName);
                return true;
            }
            return false;
        }

        static void UpdateKeywords(Material mat) {
            SetKeywordByProperty(mat, "_UseSDF", "SDF");
            SetKeywordByProperty(mat, "_UseMSDF", "MSDF");
            SetKeywordByProperty(mat, "_OverrideMSDF", "MSDF_OVERRIDE");
            SetKeywordByProperty(mat, "_UseUIAlphaClip", "UNITY_UI_ALPHACLIP");
            SetKeywordByProperty(mat, "_VRCSupport", "_VRC_SUPPORT");
            SetKeywordByProperty(mat, "_MirrorFlip", "_MIRROR_FLIP");
            SetKeywordByProperty(mat, "_DoubleSided", "_DOUBLE_SIDED");
            SetKeywordByProperty(mat, "_Billboard", "_BILLBOARD");
            SetKeywordByProperty(mat, "_ScreenSpaceOverlay", "_SCREENSPACE_OVERLAY");
        }

        static void SetKeywordByProperty(Material mat, string propertyName, string keyword) {
            int id = Shader.PropertyToID(propertyName);
            if (mat.HasProperty(id) && mat.GetFloat(id) > 0.5f) {
                mat.EnableKeyword(keyword);
                return;
            }
            mat.DisableKeyword(keyword);
        }

        static bool IsPropertyOn(MaterialProperty prop) {
            return prop != null && prop.floatValue > 0.5f;
        }

        string Loc(string key, string fallback) {
            if (locale != null) {
                var v = locale[key];
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return fallback;
        }
    }
}
