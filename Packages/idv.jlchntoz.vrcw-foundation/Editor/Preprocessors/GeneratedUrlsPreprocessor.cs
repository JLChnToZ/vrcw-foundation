using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using JLChnToZ.Regex2Pattern;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class GeneratedUrlsPreprocessor : UdonSharpPreProcessor {
        readonly Dictionary<string, IGenerator> mappers = new Dictionary<string, IGenerator>();
        readonly Dictionary<(GeneratedUrlsAttribute, Type), FieldInfo> patternSourceCache =
            new Dictionary<(GeneratedUrlsAttribute, Type), FieldInfo>();
        readonly Dictionary<(GeneratedUrlMapperAttribute, Type), FieldInfo> mapperSourceCache =
            new Dictionary<(GeneratedUrlMapperAttribute, Type), FieldInfo>();
        readonly Dictionary<(GeneratedUrlMapperAttribute, Type), FieldInfo> mapperTargetCache =
            new Dictionary<(GeneratedUrlMapperAttribute, Type), FieldInfo>();

        protected override void ProcessEntry(Type type, MonoBehaviour entry, UdonBehaviour udon) {
            var changed = ProcessGenerateUrls(type, entry);
            changed |= ProcessUrlMappers(type, entry);
            if (changed) UdonSharpEditorUtility.CopyProxyToUdon(entry as UdonSharpBehaviour);
        }

        bool ProcessGenerateUrls(Type type, MonoBehaviour entry) {
            var fields = GetFields<GeneratedUrlsAttribute>(type);
            bool hasChanges = false;
            foreach (var field in fields) {
                var fieldType = field.FieldType;
                bool isStringArray = fieldType == typeof(string[]);
                bool isVRCUrlArray = fieldType == typeof(VRCUrl[]);
                bool isDataDictionary = fieldType == typeof(DataDictionary);
                if (!isStringArray && !isVRCUrlArray && !isDataDictionary) {
                    Debug.LogError($"[GeneratedUrlsPreprocessor] Field '{field.Name}' in {type} has GeneratedUrlsAttribute but is not string[], VRCUrl[], or DataDictionary (actual: {fieldType})");
                    continue;
                }
                var attr = field.GetCustomAttribute<GeneratedUrlsAttribute>();
                if (!TryFindValue<GeneratedUrlsAttribute, string>(patternSourceCache, type, attr, entry, GetPatternSourceKey, false, out var pattern))
                    pattern = attr.Pattern;
                if (string.IsNullOrEmpty(pattern)) {
                    Debug.LogError($"[GeneratedUrlsPreprocessor] No pattern provided for field '{field.Name}' in {type}");
                    continue;
                }
                if (!mappers.TryGetValue(pattern, out var generator)) {
                    try {
                        generator = Parser.Parse(pattern);
                        mappers[pattern] = generator;
                    } catch (Exception ex) {
                        Debug.LogError($"[GeneratedUrlsPreprocessor] Failed to parse pattern '{pattern}' for field '{field.Name}' in {type}: {ex.Message}");
                        continue;
                    }
                }
                int counter = attr.Limit;
                if (isDataDictionary) {
                    var dict = new DataDictionary();
                    var regex = new Regex(pattern);
                    foreach (var str in generator) {
                        if (counter-- <= 0) break;
                        var match = regex.Match(str);
                        if (!match.Success) continue;
                        var groups = match.Groups;
                        dict[groups.Count > 1 ? groups[1].Value : str] = new DataToken(new VRCUrl(str));
                    }
                    field.SetValue(entry, dict);
                } else
                    field.SetValue(entry, isStringArray ? ConvertToArray(generator, Thru, counter) : ConvertToArray(generator, ToVRCUrl, counter));
                hasChanges = true;
            }
            return hasChanges;
        }

        bool ProcessUrlMappers(Type type, MonoBehaviour entry) {
            var mappers = GetFields<GeneratedUrlMapperAttribute>(type);
            bool hasChanges = false;
            foreach (var field in mappers) {
                if (field.FieldType != typeof(DataDictionary)) {
                    Debug.LogError($"[GeneratedUrlsPreprocessor] Field '{field.Name}' in {type} has GeneratedUrlMapperAttribute but is not DataDictionary (actual: {field.FieldType})");
                    continue;
                }
                var attr = field.GetCustomAttribute<GeneratedUrlMapperAttribute>();
                if (!TryFindValue<GeneratedUrlMapperAttribute, Array>(mapperTargetCache, type, attr, entry, GetMapperTargetUrlArray, attr.ClearSourceArray, out var urlArray)) {
                    Debug.LogError($"[GeneratedUrlsPreprocessor] Failed to find TargetUrlArray field for field '{field.Name}' in {type}");
                    continue;
                }
                if (!TryFindValue<GeneratedUrlMapperAttribute, string>(mapperSourceCache, type, attr, entry, GetMapperRegexPatternSourceKey, false, out var regexPattern))
                    regexPattern = attr.RegexPattern;
                var dataDict = new DataDictionary();
                bool indexOnly = attr.IndexOnly;
                for (int i = 0, len = urlArray.Length; i < len; i++) {
                    var urlObj = urlArray.GetValue(i);
                    string urlStr = urlObj.ToString();
                    if (urlStr == null) {
                        Debug.LogError($"[GeneratedUrlsPreprocessor] Element at index {i} in TargetUrlArray '{attr.TargetUrlArray}' in {type} is not a string or VRCUrl");
                        continue;
                    }
                    if (string.IsNullOrEmpty(urlStr)) {
                        dataDict[urlStr] = indexOnly ? i : new DataToken(urlObj);
                        continue;
                    }
                    var match = Regex.Match(urlStr, regexPattern);
                    if (!match.Success) {
                        Debug.LogWarning($"[GeneratedUrlsPreprocessor] URL '{urlStr}' at index {i} in TargetUrlArray '{attr.TargetUrlArray}' in {type} does not match regex pattern '{attr.RegexPattern}'");
                        continue;
                    }
                    var key = match.Groups[1].Value;
                    dataDict[key] = i;
                }
                field.SetValue(entry, dataDict);
                hasChanges = true;
            }
            return hasChanges;
        }

        static T[] ConvertToArray<T>(IGenerator  source, Func<string, T> converter, int limit) {
            using (ListPool<T>.Get(out var list)) {
                foreach (var str in source) {
                    if (limit-- <= 0) break;
                    list.Add(converter(str));
                }
                return list.ToArray();
            }
        }

        static string Thru(string input) => input;

        static VRCUrl ToVRCUrl(string url) => new VRCUrl(url);

        static bool TryFindValue<TAttr, TResult>(
            Dictionary<(TAttr, Type), FieldInfo> cache,
            Type type,
            TAttr attribute,
            object target,
            Func<TAttr, string> getSourceKey,
            bool clearSource,
            out TResult result
        ) where TAttr : Attribute {
            if (!cache.TryGetValue((attribute, type), out var field)) {
                var sourceKey = getSourceKey(attribute);
                if (string.IsNullOrEmpty(sourceKey)) {
                    result = default;
                    return false;
                }
                field = type.GetField(sourceKey, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) {
                    result = default;
                    return false;
                }
            }
            if (field.GetValue(target) is TResult candidate) {
                result = candidate;
                if (clearSource) field.SetValue(target, type.IsValueType ? Activator.CreateInstance(type) : null);
                return true;
            }
            result = default;
            return false;
        }

        static string GetPatternSourceKey(GeneratedUrlsAttribute attr) => attr.PatternSourceProperty;

        static string GetMapperRegexPatternSourceKey(GeneratedUrlMapperAttribute attr) => attr.RegexPatternSourceProperty;

        static string GetMapperTargetUrlArray(GeneratedUrlMapperAttribute attr) => attr.TargetUrlArray;
    }
}
