﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Text;
using System.Collections.Generic;
using JLChnToZ.VRC.Foundation.ThirdParties.LitJson;
#endif

namespace JLChnToZ.VRC.Foundation.I18N {
    /// <summary>
    /// A runtime language manager
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("JLChnToZ VRCW Foundation/Locales/Language Manager")]
    [DefaultExecutionOrder(0)]
    [HelpURL("https://github.com/JLChnToZ/VVMW/blob/main/Packages/idv.jlchntoz.vvmw/README.md#locale")]
    public partial class LanguageManager : UdonSharpEventSender {
        [SerializeField, LocalizedLabel] TextAsset[] languageJsonFiles;
        [SerializeField, Multiline, LocalizedLabel] string languageJson;
        DataDictionary languages, currentLanguage;

        [FieldChangeCallback(nameof(LanguageKey))]
        string languageKey = "EN";
        string requestedUILanguage;
        string[] languageKeys;
        string[] languageNames;
        bool afterFirstRun;

        /// <summary>The language keys.</summary>
        public string[] LanguageKeys => languageKeys;
        /// <summary>The language names.</summary>
        public string[] LanguageNames => languageNames;

        /// <summary>
        /// The current language key.
        /// </summary>
        public string LanguageKey {
            get => languageKey;
            set {
                languageKey = value;
                ChangeLanguage();
            }
        }

        void OnEnable() {
            if (afterFirstRun) return;
            afterFirstRun = true;
            if (VRCJson.TryDeserializeFromJson(languageJson, out DataToken rawLanguages) && rawLanguages.TokenType == TokenType.DataDictionary) {
                languages = rawLanguages.DataDictionary;
                languageKeys = new string[languages.Count];
                languageNames = new string[languages.Count];
                var keys = languages.GetKeys();
                for (int i = 0, count = keys.Count; i < count; i++) {
                    var currentLanguageKey = keys[i].String;
                    languageKeys[i] = keys[i].String;
                    if (languages.TryGetValue(currentLanguageKey, TokenType.DataDictionary, out DataToken token) &&
                        token.DataDictionary.TryGetValue("_name", TokenType.String, out token)) {
                        languageNames[i] = token.String;
                        continue;
                    }
                    languageNames[i] = currentLanguageKey;
                }
                DetectLanguage(VRCPlayerApi.GetCurrentLanguage());
            } else
                Debug.LogError("Failed to parse language json.");
        }

        void DetectLanguage(string uiLanguage) {
            if (requestedUILanguage == uiLanguage) return;
            requestedUILanguage = uiLanguage;
            var localTimeZone = TimeZoneInfo.Local.Id;
            int hasMatchingLanguage = 0;
            for (int i = 0, count = languageKeys.Length; i < count; i++) {
                var currentLanguageKey = languageKeys[i];
                if (!languages.TryGetValue(currentLanguageKey, TokenType.DataDictionary, out DataToken language))
                    continue;
                if (hasMatchingLanguage < 2 && language.DataDictionary.TryGetValue("_vrclang", out DataToken langToken)) {
                    switch (langToken.TokenType) {
                        case TokenType.String:
                            if (langToken.String == uiLanguage) {
                                languageKey = currentLanguageKey;
                                hasMatchingLanguage = 2;
                            }
                            break;
                        case TokenType.DataList:
                            if (langToken.DataList.Contains(uiLanguage)) {
                                languageKey = currentLanguageKey;
                                hasMatchingLanguage = 2;
                            }
                            break;
                    }
                }
                if (hasMatchingLanguage < 1 && language.DataDictionary.TryGetValue("_timezone", out DataToken timezoneToken))
                    switch (timezoneToken.TokenType) {
                        case TokenType.String:
                            if (timezoneToken.String == localTimeZone) {
                                languageKey = currentLanguageKey;
                                hasMatchingLanguage = 1;
                            }
                            break;
                        case TokenType.DataList:
                            if (timezoneToken.DataList.Contains(localTimeZone)) {
                                languageKey = currentLanguageKey;
                                hasMatchingLanguage = 1;
                            }
                            break;
                    }
            }
            ChangeLanguage();
        }

        /// <summary>
        /// Get the locale string.
        /// </summary>
        /// <param name="key">The key of the locale.</param>
        /// <returns>The locale string.</returns>
        public string GetLocale(string key) {
            if (string.IsNullOrEmpty(key)) return "";
            if (!Utilities.IsValid(currentLanguage)) return key;
            if (currentLanguage.TryGetValue(key, TokenType.String, out DataToken token))
                return token.String;
            return key;
        }

        void ChangeLanguage() {
            if (!languages.TryGetValue(languageKey, TokenType.DataDictionary, out DataToken currentLanguageWrapped))
                return;
            currentLanguage = currentLanguageWrapped.DataDictionary;
            SendEvent("_OnLanguageChanged");
        }

#if VRCSDK_3_7_0_OR_NEWER
        public override void OnLanguageChanged(string language) => DetectLanguage(language);
#else
        [NonSerialized]
        public string onLanguageChangedLanguage;
        public void _onLanguageChanged() => DetectLanguage(onLanguageChangedLanguage);
#endif
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    public partial class LanguageManager : ISingleton<LanguageManager> {
        void ISingleton<LanguageManager>.Merge(LanguageManager[] others) {
            MergeTargets(others);
            var jsonTexts = new List<string>();
            foreach (var languageManager in others) {
                if (!Utilities.IsValid(languageManager)) continue;
                var jsonFile = languageManager.languageJsonFiles;
                if (Utilities.IsValid(jsonFile)) 
                    foreach (var textAsset in jsonFile) {
                        var text = textAsset.text;
                        if (!string.IsNullOrEmpty(text))
                            jsonTexts.Add(text);
                    }
                var additionalJson = languageManager.languageJson;
                if (!string.IsNullOrEmpty(additionalJson))
                    jsonTexts.Add(additionalJson);
            }
            var langMap = new Dictionary<string, LanguageEntry>();
            var defaultLanguageMapping = new Dictionary<string, string>();
            var keyStack = new List<object>();
            var allLanguageKeys = new HashSet<string>();
            foreach (var json in jsonTexts)
                ParseFromJson(json, keyStack, defaultLanguageMapping, allLanguageKeys, langMap);
            languageJson = WriteToJson(langMap, defaultLanguageMapping);
            languageJsonFiles = new TextAsset[0];
        }

        internal static Dictionary<string, LanguageEntry> ParseFromJson(
            string json,
            List<object> keyStack = null,
            Dictionary<string, string> defaultLanguageMapping = null,
            HashSet<string> allLanguageKeys = null,
            Dictionary<string, LanguageEntry> langMap = null
        ) {
            if (!Utilities.IsValid(langMap)) langMap = new Dictionary<string, LanguageEntry>();
            if (!Utilities.IsValid(keyStack)) keyStack = new List<object>();
            else keyStack.Clear();
            var reader = new JsonReader(json);
            LanguageEntry currentEntry = null;
            while (reader.Read())
                switch (reader.Token) {
                    case JsonToken.ObjectStart:
                        switch (keyStack.Count) {
                            case 1:
                                if (keyStack[0] is string key && !langMap.TryGetValue(key, out currentEntry))
                                    langMap[key] = currentEntry = new LanguageEntry();
                                break;
                        }
                        keyStack.Add(null);
                        break;
                    case JsonToken.ArrayStart:
                        keyStack.Add(0);
                        break;
                    case JsonToken.ObjectEnd:
                    case JsonToken.ArrayEnd:
                        keyStack.RemoveAt(keyStack.Count - 1);
                        break;
                    case JsonToken.PropertyName:
                        keyStack[keyStack.Count - 1] = reader.Value;
                        break;
                    case JsonToken.String:
                        switch (keyStack.Count) {
                            case 2: {
                                if (Utilities.IsValid(currentEntry) && keyStack[1] is string key) {
                                    var strValue = (string)reader.Value;
                                    switch (key) {
                                        case "_name":
                                            currentEntry.name = strValue;
                                            break;
                                        case "_vrclang":
                                            currentEntry.vrcName = strValue;
                                            break;
                                        case "_timezone":
                                            currentEntry.timezones.Add(strValue);
                                            break;
                                        default:
                                            currentEntry.languages[key] = strValue;
                                            allLanguageKeys?.Add(key);
                                            if (Utilities.IsValid(defaultLanguageMapping) &&
                                                !defaultLanguageMapping.ContainsKey(key))
                                                defaultLanguageMapping[key] = strValue;
                                            break;
                                    }
                                }
                                break;
                            }
                            case 3: {
                                if (Utilities.IsValid(currentEntry) && keyStack[1] is string key && key == "_timezone" && keyStack[2] is int)
                                    currentEntry.timezones.Add(reader.Value.ToString());
                                break;
                            }
                        }
                        goto default;
                    default:
                        if (keyStack.Count > 0 && keyStack[keyStack.Count - 1] is int index)
                            keyStack[keyStack.Count - 1] = index + 1;
                        break;
                }
            return langMap;
        }

        internal static string WriteToJson(
            Dictionary<string, LanguageEntry> langMap,
            Dictionary<string, string> defaultLanguageMapping = null,
            bool prettyPrint = false
        ) {
            var sb = new StringBuilder();
            var jsonWriter = new JsonWriter(sb) {
                PrettyPrint = prettyPrint,
            };
            jsonWriter.WriteObjectStart();
            foreach (var kv in langMap) {
                jsonWriter.WritePropertyName(kv.Key);
                jsonWriter.WriteObjectStart();
                var lang = kv.Value;
                if (!string.IsNullOrEmpty(lang.name)) {
                    jsonWriter.WritePropertyName("_name");
                    jsonWriter.Write(lang.name);
                }
                if (!string.IsNullOrEmpty(lang.vrcName)) {
                    jsonWriter.WritePropertyName("_vrclang");
                    jsonWriter.Write(lang.vrcName);
                }
                if (lang.timezones.Count > 0) {
                    jsonWriter.WritePropertyName("_timezone");
                    if (lang.timezones.Count == 1)
                        jsonWriter.Write(lang.timezones[0]);
                    else {
                        jsonWriter.WriteArrayStart();
                        foreach (var timezone in lang.timezones) jsonWriter.Write(timezone);
                        jsonWriter.WriteArrayEnd();
                    }
                }
                foreach (var langEntry in lang.languages) {
                    jsonWriter.WritePropertyName(langEntry.Key);
                    jsonWriter.Write(langEntry.Value);
                }
                if (Utilities.IsValid(defaultLanguageMapping))
                    foreach (var defaultLang in defaultLanguageMapping)
                        if (!lang.languages.ContainsKey(defaultLang.Key)) {
                            jsonWriter.WritePropertyName(defaultLang.Key);
                            jsonWriter.Write(defaultLang.Value);
                        }
                jsonWriter.WriteObjectEnd();
            }
            jsonWriter.WriteObjectEnd();
            return sb.ToString();
        }
    }

    internal class LanguageEntry {
        public string name;
        public string vrcName;
        public readonly List<string> timezones = new List<string>();
        public readonly Dictionary<string, string> languages = new Dictionary<string, string>();
    }
#endif
}