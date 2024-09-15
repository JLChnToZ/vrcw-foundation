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
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class EditorI18NSource : Attribute {
        public string LanguageAssetPath { get; set; }
        public string LanguageAssetGUID { get; set; }
    }

    [InitializeOnLoad]
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

        public string CurrentLanguage {
            get => currentLanguage;
            set {
                currentLanguage = value;
                #if UNITY_EDITOR
                EditorPrefs.SetString(PREF_KEY, value);
                #endif
            }
        }

        public int LanguageIndex {
            get => Array.IndexOf(languageKeys, currentLanguage);
            set => CurrentLanguage = languageKeys[value];
        }

        public string[] LanguageNames => languageNames;

        public static EditorI18N Instance => instance;

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

        public string GetOrDefault(string key, string defaultValue = null) {
            var value = this[key];
            return string.IsNullOrEmpty(value) ? defaultValue ?? key : value;
        }

        public void Reload() {
            i18nDict.Clear();
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
                    if (!i18nDict.TryGetValue(lang, out var langDict))
                        i18nDict[lang] = langDict = new Dictionary<string, string>(comparer);
                    var langData = jsonData[lang];
                    string langName = null;
                    foreach (var key in langData.Keys)
                        switch (key) {
                            case "_alias":
                                foreach (JsonData aliasKey in langData[key])
                                    alias[(string)aliasKey] = lang;
                                break;
                            case "_name":
                                langName = (string)langData[key];
                                break;
                            default:
                                langDict[key] = (string)langData[key];
                                break;
                        }
                    if (!string.IsNullOrEmpty(lang) && (!string.IsNullOrEmpty(langName) || !keyNameMap.ContainsKey(lang)))
                        keyNameMap[lang] = langName ?? lang;
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
        }
    }
}