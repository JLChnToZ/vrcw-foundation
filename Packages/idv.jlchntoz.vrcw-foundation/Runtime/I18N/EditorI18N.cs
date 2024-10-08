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
        public string LanguageAssetPath { get; set; }
        /// <summary>
        /// The GUID of the language asset.
        /// </summary>
        public string LanguageAssetGUID { get; set; }
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
                var source = assembly.GetCustomAttribute<EditorI18NSource>();
                if (source == null) continue;
                sources.Add(source);
            }
            appDomain.AssemblyLoad += OnAssemblyLoad;
            instance.Reload();
        }

        static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
            var source = args.LoadedAssembly.GetCustomAttribute<EditorI18NSource>();
            if (source == null) return;
            sources.Add(source);
            instance.Reload();
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
            foreach (var langSource in sources) {
                TextAsset i18nData = null;
                string path = langSource.LanguageAssetPath;
                if (string.IsNullOrEmpty(path) &&
                    !string.IsNullOrEmpty(langSource.LanguageAssetGUID)) {
                    var path2 = AssetDatabase.GUIDToAssetPath(langSource.LanguageAssetGUID);
                    if (!string.IsNullOrEmpty(path2)) path = path2;
                }
                if (!string.IsNullOrEmpty(path))
                    i18nData = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (i18nData == null) continue;
                var jsonData = JsonMapper.ToObject(i18nData.text);
                var comparer = StringComparer.OrdinalIgnoreCase;
                foreach (var lang in jsonData.Keys) {
                    if (!alias.TryGetValue(lang, out var realLang)) realLang = lang;
                    if (!i18nDict.TryGetValue(realLang, out var langDict))
                        i18nDict[realLang] = langDict = new Dictionary<string, string>(comparer);
                    var langData = jsonData[realLang];
                    foreach (var key in langData.Keys)
                        switch (key) {
                            case "_alias":
                                foreach (JsonData aliasKey in langData[key])
                                    alias[(string)aliasKey] = realLang;
                                break;
                            case "_name":
                                keyNameMap[realLang] = (string)langData[key];
                                break;
                            default:
                                langDict[key] = (string)langData[key];
                                break;
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
                    if (string.IsNullOrEmpty(src.LanguageAssetPath)) {
                        if (!string.IsNullOrEmpty(src.LanguageAssetGUID)) {
                            var path = AssetDatabase.GUIDToAssetPath(src.LanguageAssetGUID);
                            if (interestedPaths.Contains(path)) {
                                shouldReload = true;
                                break;
                            }
                        }
                    } else if (interestedPaths.Contains(src.LanguageAssetPath)) {
                        shouldReload = true;
                        break;
                    }
                }
                if (shouldReload) instance.Reload();
            }
        }
#endif
    }
}