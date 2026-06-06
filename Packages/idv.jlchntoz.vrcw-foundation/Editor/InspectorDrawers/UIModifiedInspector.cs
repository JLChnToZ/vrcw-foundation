using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JLChnToZ.VRC.Foundation.Editors;
using JLChnToZ.VRC.Foundation.I18N;
using JLChnToZ.VRC.Foundation.I18N.Editors;

namespace JLChnToZ.VRCW.Foundation.Editor {
    public class UIModifiedInspector : ShaderGUI {
        static bool hasInitialized;
        static readonly Dictionary<Shader, Shader> geometryShaderMap = new Dictionary<Shader, Shader>();
        static readonly Dictionary<Shader, Shader> nonGeometryShaderMap = new Dictionary<Shader, Shader>();
        static readonly Dictionary<int, string> materialToKeywordMap = new Dictionary<int, string>();
        static readonly HashSet<int> keywordsRequiringGeometry = new HashSet<int>();
        static readonly string[] sdfModes = new string[3];
        static readonly string[] specialRenderModes = new string[4];
        EditorI18N locale;

        MaterialEditor editor;
        MaterialProperty[] properties;
        GUIContent[] twoContents;
        float[] twoFloats;

        static void Initialize() {
            if (hasInitialized) return;
            hasInitialized = true;
            AddKeywordMapping("_UseSDF", "SDF");
            AddKeywordMapping("_UseMSDF", "MSDF");
            AddKeywordMapping("_OverrideMSDF", "MSDF_OVERRIDE");
            AddKeywordMapping("_UseUIAlphaClip", "UNITY_UI_ALPHACLIP");
            AddKeywordMapping("_VRCSupport", "_VRC_SUPPORT");
            AddKeywordMapping("_MirrorFlip", "_MIRROR_FLIP", true);
            AddKeywordMapping("_DoubleSided", "_DOUBLE_SIDED", true);
            AddKeywordMapping("_Billboard", "_BILLBOARD");
            AddKeywordMapping("_ScreenSpaceOverlay", "_SCREENSPACE_OVERLAY");
            AddKeywordMapping("_DistanceFade", "_DISTANCE_FADE");
            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets", "Packages" })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("-NoGeom.shader")) continue;
                var nonGeomShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                var geomPath = path.Replace("-NoGeom", "");
                var geomShader = AssetDatabase.LoadAssetAtPath<Shader>(geomPath);
                if (geomShader != null) {
                    geometryShaderMap[nonGeomShader] = geomShader;
                    nonGeometryShaderMap[geomShader] = nonGeomShader;
                }
            }
        }

        static void AddKeywordMapping(string propertyName, string keyword, bool requiresGeometry = false) {
            var id = Shader.PropertyToID(propertyName);
            materialToKeywordMap[id] = keyword;
            if (requiresGeometry) keywordsRequiringGeometry.Add(id);
        }

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

            DrawDistanceFadeSettings();
            EditorGUILayout.Space();

            DrawExperimentalSettings();

            // Other Settings
            FindAndDrawProperty("_UseUIAlphaClip", "UIModified.UseAlphaClip");
            FindAndDrawProperty("_Cull", "UIModified.CullMode");
            FindAndDrawProperty("_ColorMask", "UIModified.ColorMask");
            FindAndDrawProperty("_StencilComp", "UIModified.Stencil.Comp");
            FindAndDrawProperty("_Stencil", "UIModified.Stencil.ID");
            FindAndDrawProperty("_StencilOp", "UIModified.Stencil.Operation");
            FindAndDrawProperty("_StencilWriteMask", "UIModified.Stencil.WriteMask");
            FindAndDrawProperty("_StencilReadMask", "UIModified.Stencil.ReadMask");
            FindAndDrawProperty("unity_GUIZTestMode", "UIModified.ZTestMode");
            FindAndDrawProperty("_ZWrite", "UIModified.ZWrite");

            editor.RenderQueueField();
            editor.EnableInstancingField();
            editor.DoubleSidedGIField();

            if (changed.changed) {
                // Update keywords for all target materials
                foreach (var obj in editor.targets) {
                    var mat = obj as Material;
                    if (mat == null) continue;
                    UpdateKeywordsAndShaders(mat);
                }
            }
        }

        void DrawMainSettings() {
            var mainTex = FindProperty("_MainTex");
            var color = FindProperty("_Color") ?? FindProperty("_FaceColor");
            if (mainTex != null && color != null)
                editor.TexturePropertySingleLine(locale.GetLocalizedContent("UIModified.SpriteAndColor"), mainTex, color);
            if (FindAndDrawProperty("_TextureWidth") |
                FindAndDrawProperty("_TextureHeight") |
                FindAndDrawProperty("_GradientScale") |
                FindAndDrawProperty("_WeightNormal") |
                FindAndDrawProperty("_WeightBold") |
                FindAndDrawProperty("_ScaleRatioA")) {
                EditorGUILayout.HelpBox(locale["UIModified.Help.FontSettings"], MessageType.Info);
            }
        }

        void DrawSDFSettings() {
            var useSdf = FindProperty("_UseSDF");
            var useMsdf = FindProperty("_UseMSDF");
            if (useSdf == null && useMsdf == null) return;
            EditorGUILayout.LabelField(locale.GetLocalizedContent("UIModified.SDFSettings"), EditorStyles.boldLabel);
            int sdfMode = 0;
            if (IsPropertyOn(useSdf))
                sdfMode = 1;
            else if (IsPropertyOn(useMsdf))
                sdfMode = 2;
            using (var check = new EditorGUI.ChangeCheckScope()) {
                sdfModes[0] = locale["UIModified.SDFMode.NonSDF"] ?? "";
                sdfModes[1] = locale["UIModified.SDFMode.SDF"] ?? "";
                sdfModes[2] = locale["UIModified.SDFMode.MSDF"] ?? "";
                sdfMode = EditorGUILayout.Popup(locale.GetLocalizedContent("UIModified.TextureType"), sdfMode, sdfModes);
                if (check.changed) {
                    if (useSdf != null) useSdf.floatValue = (sdfMode == 1) ? 1f : 0f;
                    if (useMsdf != null) useMsdf.floatValue = (sdfMode == 2) ? 1f : 0f;
                }
            }
            if (sdfMode != 0) {
                var overrideMsdf = FindProperty("_OverrideMSDF");
                if (overrideMsdf != null) editor.ShaderProperty(overrideMsdf, locale.GetLocalizedContent("UIModified.UseOverrideTexture"));
                if (overrideMsdf != null && overrideMsdf.floatValue > 0.5f) {
                    var msdfTex = FindProperty("_MSDFTex");
                    if (msdfTex != null) editor.TexturePropertySingleLine(locale.GetLocalizedContent("UIModified.OverrideTexture"), msdfTex);
                }
                FindAndDrawProperty("_PixelRange", "UIModified.PixelRange");
                FindAndDrawProperty("_SDFThreshold", "UIModified.Threshold");
            }
        }

        void DrawVRCSettings() {
            EditorGUILayout.LabelField(locale.GetLocalizedContent("UIModified.VRChatSettings"), EditorStyles.boldLabel);
            var renderMode = FindProperty("_RenderMode");
            if (renderMode != null) {
                using var check = new EditorGUI.ChangeCheckScope();
                editor.ShaderProperty(renderMode, locale.GetLocalizedContent("UIModified.VisibleModes"));
                if (check.changed) {
                    var vrcSupport = FindProperty("_VRCSupport");
                    if (vrcSupport != null) vrcSupport.floatValue = (renderMode.intValue & 0xFFF) != 0xFFF ? 1f : 0f;
                }
            }
            FindAndDrawProperty("_MirrorFlip", "UIModified.FlipInMirror");
        }

        void DrawExperimentalSettings() {
            EditorGUILayout.LabelField(locale.GetLocalizedContent("UIModified.ExperimentalSettings"), EditorStyles.boldLabel);
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
            using (var check = new EditorGUI.ChangeCheckScope()) {
                specialRenderModes[0] = locale["UIModified.RenderMode.Normal"] ?? "";
                specialRenderModes[1] = locale["UIModified.RenderMode.DoubleSided"] ?? "";
                specialRenderModes[2] = locale["UIModified.RenderMode.Billboard"] ?? "";
                specialRenderModes[3] = locale["UIModified.RenderMode.ScreenSpaceOverlay"] ?? "";
                renderMode = EditorGUILayout.Popup(locale.GetLocalizedContent("UIModified.RenderMode"), renderMode, specialRenderModes);
                if (check.changed) {
                    if (doubleSided != null) doubleSided.floatValue = (renderMode == 1) ? 1f : 0f;
                    if (billboard != null) billboard.floatValue = (renderMode == 2) ? 1f : 0f;
                    if (screenSpace != null) screenSpace.floatValue = (renderMode == 3) ? 1f : 0f;
                }
            }
            switch (renderMode) {
                case 1:
                    EditorGUILayout.HelpBox(locale["UIModified.Help.DoubleSided"], MessageType.Info);
                    break;
                case 2:
                    EditorGUILayout.HelpBox(locale["UIModified.Help.Billboard"], MessageType.Info);
                    break;
                case 3: {
                        var canvasRect = FindProperty("_CanvasRect");
                        if (canvasRect != null) {
                            MaterialEditor.BeginProperty(canvasRect);
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var value = canvasRect.vectorValue;
                                var rectValue = new Rect(value.x, value.y, value.z, value.w);
                                rectValue = EditorGUILayout.RectField(locale.GetLocalizedContent("UIModified.CanvasRect"), rectValue);
                                if (check.changed) canvasRect.vectorValue = new Vector4(rectValue.x, rectValue.y, rectValue.width, rectValue.height);
                            }
                            MaterialEditor.EndProperty();
                        }
                        var aspect = FindProperty("_AspectRatioMatch");
                        if (aspect != null) {
                            MaterialEditor.BeginProperty(aspect);
                            EditorGUILayout.PrefixLabel(locale.GetLocalizedContent("UIModified.AspectRatioMatch"));
                            using (new EditorGUILayout.HorizontalScope())
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var value = aspect.floatValue;
                                GUILayout.Label(locale.GetLocalizedContent("UIModified.WidthLabel"));
                                value = EditorGUILayout.Slider(value, 0f, 1f, GUILayout.ExpandWidth(true));
                                GUILayout.Label(locale.GetLocalizedContent("UIModified.HeightLabel"));
                                if (check.changed) aspect.floatValue = value;
                            }
                            MaterialEditor.EndProperty();
                        }
                    }
                    break;
            }
        }

        void DrawDistanceFadeSettings() {
            var distanceFade = FindProperty("_DistanceFade");
            if (distanceFade == null) return;
            editor.ShaderProperty(distanceFade, locale.GetLocalizedContent("UIModified.DistanceFade.Enable"));
            if (distanceFade.floatValue > 0.5f) {
                var distanceFadeParams = FindProperty("_DistanceFadeParams");
                if (distanceFadeParams != null) {
                    MaterialEditor.BeginProperty(distanceFadeParams);
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        var value = distanceFadeParams.vectorValue;
                        twoContents ??= new[] { new GUIContent(), new GUIContent() };
                        twoFloats ??= new float[2];
                        locale.GetLocalizedContent("UIModified.DistanceFade.Start", twoContents[0]);
                        locale.GetLocalizedContent("UIModified.DistanceFade.End", twoContents[1]);
                        twoFloats[0] = value.x;
                        twoFloats[1] = value.y;
                        EditorGUI.MultiFloatField(EditorGUILayout.GetControlRect(false), twoContents, twoFloats);
                        if (check.changed) distanceFadeParams.vectorValue = new Vector4(twoFloats[0], twoFloats[1], value.z, value.w);
                    }
                    MaterialEditor.EndProperty();
                }
            }
        }

        MaterialProperty FindProperty(string name) => FindProperty(name, properties, false);

        bool FindAndDrawProperty(string name, string displayName = null) {
            var prop = FindProperty(name);
            if (prop == null) return false;
            editor.ShaderProperty(prop,
                string.IsNullOrEmpty(displayName) ?
                    Utils.GetTempContent(prop.displayName) :
                    locale.GetLocalizedContent(displayName)
            );
            return true;
        }

        static void UpdateKeywordsAndShaders(Material mat) {
            Initialize();
            bool useGeom = false;
            foreach (var (id, keyword) in materialToKeywordMap) {
                if (mat.HasProperty(id) && mat.GetFloat(id) > 0.5f) {
                    mat.EnableKeyword(keyword);
                    if (keywordsRequiringGeometry.Contains(id)) useGeom = true;
                    continue;
                }
                mat.DisableKeyword(keyword);
            }
            var currentShader = mat.shader;
            if ((useGeom ? geometryShaderMap : nonGeometryShaderMap).TryGetValue(currentShader, out var targetShader)) {
                if (mat.parent != null) mat.parent = null;
                mat.shader = targetShader;
            }
        }

        static void SetKeywordByProperty(Material mat, string propertyName, string keyword) {
            int id = Shader.PropertyToID(propertyName);
            if (mat.HasProperty(id) && mat.GetFloat(id) > 0.5f) {
                mat.EnableKeyword(keyword);
                return;
            }
            mat.DisableKeyword(keyword);
        }

        static bool IsPropertyOn(MaterialProperty prop) => prop != null && prop.floatValue > 0.5f;
    }
}
