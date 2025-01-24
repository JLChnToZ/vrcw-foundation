using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using JLChnToZ.VRC.Foundation.ThirdParties.LitJson;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JLChnToZ.VRC.Foundation.I18N {
    /// <summary>
    /// Add this attribute to assembly to define a source of I18N data you want to automatically load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class EditorI18NSource : Attribute {
        /// <summary>
        /// The path of the language asset.
        /// </summary>
        /// <remarks>
        /// If this is not set, the GUID will be used to load the asset.
        /// </remarks>
        public string LanguageAssetPath {
            get => LanguageAssetPaths != null && LanguageAssetPaths.Length > 0 ? LanguageAssetPaths[0] : null;
            set {
                if (LanguageAssetPaths == null || LanguageAssetPaths.Length == 0)
                    LanguageAssetPaths = new string[1];
                LanguageAssetPaths[0] = value;
            }
        }
        public string[] LanguageAssetPaths { get; set; }
        /// <summary>
        /// The GUID of the language asset.
        /// </summary>
        public string LanguageAssetGUID {
            get => LanguageAssetGUIDs != null && LanguageAssetGUIDs.Length > 0 ? LanguageAssetGUIDs[0] : null;
            set {
                if (LanguageAssetGUIDs == null || LanguageAssetGUIDs.Length == 0)
                    LanguageAssetGUIDs = new string[1];
                LanguageAssetGUIDs[0] = value;
            }
        }
        public string[] LanguageAssetGUIDs { get; set; }
    }

    /// <summary>
    /// I18N manager for editor.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class EditorI18N {
        const string PREF_KEY = "vrcw.lang";
        const string DEFAULT_LANGUAGE = "en";
        static EditorI18N instance;
        static readonly HashSet<EditorI18NSource> sources = new HashSet<EditorI18NSource>();
        string[] languageNames;
        string[] languageKeys;
        readonly Dictionary<string, Dictionary<string, string>> i18nDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string currentLanguage;

        /// <summary>
        /// The current language.
        /// </summary>
        public string CurrentLanguage {
            get => currentLanguage;
            set {
                currentLanguage = value;
                #if UNITY_EDITOR
                EditorPrefs.SetString(PREF_KEY, value);
                #endif
            }
        }

        /// <summary>
        /// The index of the current language.
        /// </summary>
        public int LanguageIndex {
            get => Array.IndexOf(languageKeys, currentLanguage);
            set => CurrentLanguage = languageKeys[value];
        }

        /// <summary>
        /// The display names of available languages.
        /// </summary>
        public string[] LanguageNames => languageNames;

        /// <summary>
        /// The singleton instance of <see cref="EditorI18N"/>.
        /// </summary>
        public static EditorI18N Instance => instance;

        /// <summary>
        /// Get the localized string by key.
        /// </summary>
        public string this[string key] {
            get {
                if (i18nDict.TryGetValue(currentLanguage, out var langDict) &&
                    langDict.TryGetValue(key, out var value))
                    return value;
                if (i18nDict.TryGetValue(DEFAULT_LANGUAGE, out langDict) &&
                    langDict.TryGetValue(key, out value))
                    return value;
                return null;
            }
        }

        static EditorI18N() {
            instance = new EditorI18N();
            var appDomain = AppDomain.CurrentDomain;
            foreach (var assembly in appDomain.GetAssemblies()) {
                var source = assembly.GetCustomAttributes<EditorI18NSource>();
                if (source == null) continue;
                sources.UnionWith(source);
            }
            appDomain.AssemblyLoad += OnAssemblyLoad;
#if UNITY_EDITOR
            EditorApplication.delayCall += instance.Reload;
#endif
        }

        static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
            var source = args.LoadedAssembly.GetCustomAttributes<EditorI18NSource>();
            if (source == null) return;
            sources.UnionWith(source);
#if UNITY_EDITOR
            EditorApplication.delayCall += instance.Reload;
#endif
        }

        /// <summary>
        /// Get the localized string by key, or return the default value if not found.
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="defaultValue">The default value to return if not found.</param>
        /// <returns>The localized string.</returns>
        public string GetOrDefault(string key, string defaultValue = null) {
            var value = this[key];
            return string.IsNullOrEmpty(value) ? defaultValue ?? key : value;
        }

        /// <summary>
        /// Reload the I18N data.
        /// </summary>
        public void Reload() {
#if UNITY_EDITOR
            i18nDict.Clear();
            alias.Clear();
            var keyNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var paths = new HashSet<string>();
            foreach (var langSource in sources) {
                var rawPaths = langSource.LanguageAssetPaths;
                if (rawPaths != null) {
                    paths.UnionWith(rawPaths);
                    continue;
                }
                var guids = langSource.LanguageAssetGUIDs;
                if (guids != null)
                    foreach (var guid in guids) {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path)) paths.Add(path);
                    }
            }
            foreach (var path in paths) {
                var i18nData = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (i18nData == null) continue;
                var jsonData = JsonMapper.ToObject(i18nData.text);
                var comparer = StringComparer.OrdinalIgnoreCase;
                foreach (var lang in jsonData.Keys) {
                    if (!alias.TryGetValue(lang, out var realLang)) realLang = lang;
                    if (!i18nDict.TryGetValue(realLang, out var langDict))
                        i18nDict[realLang] = langDict = new Dictionary<string, string>(comparer);
                    var langData = jsonData[lang];
                    foreach (var key in langData.Keys) {
                        var ld = langData[key];
                        switch (key) {
                            case "_alias":
                                foreach (JsonData aliasKey in ld) {
                                    var aliasName = (string)aliasKey;
                                    alias[aliasName] = realLang;
                                    if (!string.Equals(aliasName, realLang, StringComparison.OrdinalIgnoreCase)) {
                                        if (i18nDict.TryGetValue(aliasName, out var aliasDict) &&
                                            aliasDict != langDict) {
                                            foreach (var pair in aliasDict)
                                                langDict[pair.Key] = pair.Value;
                                            i18nDict.Remove(aliasName);
                                        }
                                        keyNameMap.Remove(aliasName);
                                    }
                                }
                                break;
                            case "_name":
                                keyNameMap[lang] = (string)ld;
                                break;
                            default:
                                switch (ld.GetJsonType()) {
                                    case JsonType.Object:
                                    case JsonType.Array: break;
                                    default:
                                        langDict[key] = ld.ToString();
                                        break;
                                }
                                break;
                        }
                    }
                    if (!keyNameMap.ContainsKey(realLang)) keyNameMap.Add(realLang, realLang);
                }
            }
            int i = 0;
            languageKeys = new string[keyNameMap.Count];
            languageNames = new string[keyNameMap.Count];
            foreach (var pair in keyNameMap) {
                languageKeys[i] = pair.Key;
                languageNames[i] = pair.Value;
                i++;
            }
            if (string.IsNullOrEmpty(currentLanguage)) {
                currentLanguage = CultureInfo.CurrentCulture.Name;
                #if UNITY_EDITOR
                currentLanguage = EditorPrefs.GetString(PREF_KEY, currentLanguage);
                #endif
            }
            if (i18nDict.ContainsKey(currentLanguage)) return;
            if (alias.TryGetValue(currentLanguage, out var aliasLang) &&
                i18nDict.ContainsKey(aliasLang)) {
                currentLanguage = aliasLang;
                return;
            }
            if (currentLanguage.Length >= 2) {
                var regionless = currentLanguage.Substring(0, 2);
                if (i18nDict.ContainsKey(regionless)) {
                    currentLanguage = regionless;
                    return;
                }
            }
            currentLanguage = DEFAULT_LANGUAGE;
#endif
        }

#if UNITY_EDITOR
        sealed class LocaleUpdateWatcher : AssetPostprocessor {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload) {
                var interestedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                interestedPaths.UnionWith(importedAssets);
                interestedPaths.UnionWith(deletedAssets);
                interestedPaths.UnionWith(movedAssets);
                interestedPaths.UnionWith(movedFromAssetPaths);
                bool shouldReload = false;
                foreach (var src in sources) {
                    if (src.LanguageAssetPaths != null)
                        foreach (var path in src.LanguageAssetPaths)
                            if (!string.IsNullOrEmpty(path) && interestedPaths.Contains(path)) {
                                shouldReload = true;
                                break;
                            }
                    if (shouldReload) break;
                    if (src.LanguageAssetGUIDs != null)
                        foreach (var guid in src.LanguageAssetGUIDs) {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(path) && interestedPaths.Contains(path)) {
                                shouldReload = true;
                                break;
                            }
                        }
                }
                if (shouldReload) instance.Reload();
            }
        }
#endif
    }
}