using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

using UnityEngine;
using UnityEngine.Events;
using UnityEditor.Events;
using UnityEditor.Callbacks;

using VRC.Udon;
using VRC.Udon.Editor;
using UdonSharp;
using JLChnToZ.VRC.Foundation.Resolvers;

using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class BindEventPreprocessor : UdonSharpPreProcessor {
        static readonly Regex regexCompositeFormat = new Regex(@"\{(\d+)[,:]?[^\}]*\}", RegexOptions.Compiled);
        static readonly Dictionary<string, string> typeNameMapping = new Dictionary<string, string>();
        static readonly Dictionary<BindEventAttribute, Resolver> resolverCache = new Dictionary<BindEventAttribute, Resolver>();
        static bool hasTypeNameMappingInit;

        protected override void ProcessEntry(Type type, MonoBehaviour entry, UdonBehaviour udon) {
            UnityAction<string> call = udon.SendCustomEvent;
            ProcessEntry(type, udon, call);
            var fieldInfos = GetFields<BindEventAttribute>(type);
            foreach (var field in fieldInfos) {
                var targetObj = field.GetValue(entry);
                if (targetObj is Array array)
                    for (int i = 0, length = array.GetLength(0); i < length; i++)
                        ProcessEntry(array.GetValue(i) as UnityObject, field, call, i);
                else if (targetObj is UnityObject unityObject)
                    ProcessEntry(unityObject, field, call, 0);
            }
        }

        static void ProcessEntry(Type type, Component component, UnityAction<string> call) {
            foreach (var attribute in type.GetCustomAttributes<BindEventAttribute>(true)) {
                var srcType = attribute.SourceType;
                if (srcType == null) continue;
                var targetObjs = component.GetComponents(srcType);
                for (int i = 0; i < targetObjs.Length; i++)
                    BindSingleEvent(targetObjs[i], srcType, attribute, call, i);
            }
        }

        static void ProcessEntry(UnityObject targetObj, MemberInfo member, UnityAction<string> call, int index) {
            if (targetObj == null) return;
            var srcType = targetObj.GetType();
            foreach (var attribute in member.GetCustomAttributes<BindEventAttribute>(true))
                BindSingleEvent(targetObj, attribute.SourceType ?? srcType, attribute, call, index);
        }

        static void BindSingleEvent(UnityObject targetObj, Type srcType, BindEventAttribute attribute, UnityAction<string> call, int index) {
            if (!resolverCache.TryGetValue(attribute, out var resolver)) {
                var srcPath = attribute.Source;
                int hashIndex = srcPath.LastIndexOf("#");
                resolver = Resolver.Create();
                if (hashIndex < 0) {
                    resolver.WithType(srcType);
                    resolver.WithPath(srcPath);
                } else {
                    resolver.WithPath(srcPath.Substring(0, hashIndex));
                    resolver.WithType(srcType);
                    resolver.WithPath(srcPath.Substring(hashIndex + 1));
                }
                resolver.WithType<UnityEventBase>();
                resolverCache[attribute] = resolver;
            }
            if (resolver.TryResolve(targetObj, out var otherObj) && otherObj is UnityEventBase callback) {
                var targetEventName = string.Format(attribute.Destination, index, targetObj.name);
                if (!hasTypeNameMappingInit) {
                    foreach (var def in UdonEditorManager.Instance.GetNodeDefinitions()) {
                        if (!def.fullName.StartsWith("Event_")) continue;
                        typeNameMapping[def.fullName.Substring(6)] = $"_{char.ToLower(def.fullName[6])}{def.fullName.Substring(7)}";
                    }
                    hasTypeNameMappingInit = true;
                }
                if (typeNameMapping.TryGetValue(targetEventName, out var mappedEventName))
                    targetEventName = mappedEventName;
                UnityEventTools.AddStringPersistentListener(callback, call, targetEventName);
            }
        }

        [DidReloadScripts]
        static void Validate() {
            GatherMonoScripts();
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())) {
                foreach (var attribute in type.GetCustomAttributes<BindEventAttribute>(true)) ValidateType(type, attribute);
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                foreach (var attribute in field.GetCustomAttributes<BindEventAttribute>(true)) ValidateType(type, attribute, field);
            }
        }

        static void ValidateType(Type type, BindEventAttribute attribute, FieldInfo field = null) {
            var messagePrefix = field != null ? $"BindEventAttribute on {type.FullName}.{field.Name}: " : $"BindEventAttribute on {type.FullName}: ";
            scriptMap.TryGetValue(type, out var currentScript);
            if (!type.IsSubclassOf(typeof(UdonSharpBehaviour))) {
                Debug.LogError($"{messagePrefix} Type is not derived from UdonSharpBehaviour, which will not have any effect.", currentScript);
                return;
            }
            var srcName = attribute.Source;
            if (string.IsNullOrEmpty(srcName)) {
                Debug.LogError($"{messagePrefix} Source is empty.", currentScript);
                return;
            }
            int hashIndex = srcName.IndexOf('#');
            bool srcIsAPath = hashIndex >= 0;
            if (srcIsAPath) srcName = srcName.Substring(hashIndex + 1);
            var destName = attribute.Destination;
            if (string.IsNullOrEmpty(destName)) {
                Debug.LogError($"{messagePrefix} Destination is empty.", currentScript);
                return;
            }
            var srcType = attribute.SourceType;
            if (srcType == null) {
                if (field != null)
                    srcType = field.FieldType;
                else
                    Debug.LogError($"{messagePrefix} SourceType is empty.", currentScript);
                return;
            }
            if (srcType.IsArray) srcType = srcType.GetElementType();
            var srcProperty = srcType.GetProperty(srcName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (srcProperty == null) {
                var srcField = srcType.GetField(srcName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (srcField == null) {
                    Debug.LogError($"{messagePrefix} Source {srcType.FullName}.{srcName} does not exists.", currentScript);
                    return;
                }
                srcType = srcField.FieldType;
            } else
                srcType = srcProperty.PropertyType;
            if (!srcType.IsSubclassOf(typeof(UnityEventBase))) {
                Debug.LogError($"{messagePrefix} Source {srcType.FullName}.{srcName} is not an Unity event. Only Unity events can be bound.", currentScript);
                return;
            }
            var regex = regexCompositeFormat.IsMatch(destName) ? new Regex(regexCompositeFormat.Replace(destName, ReplaceRegex), RegexOptions.Compiled) : null;
            bool hasAnyMatch = false;
            foreach (var member in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                if (regex != null ? regex.IsMatch(member.Name) : destName == member.Name) {
                    hasAnyMatch = true;
                    if (member.GetParameters().Length > 0)
                        Debug.LogWarning($"{messagePrefix} Destination {member.Name} has parameters, which still can be called, but will cause unexpected behavior.", currentScript);
                    if (!member.IsPublic)
                        Debug.LogError($"{messagePrefix} Destination {member.Name} is not public, which cannot be called from outside.", currentScript);
                }
            if (!hasAnyMatch)
                Debug.LogError($"{messagePrefix} None of destination methods matches {destName}.", currentScript);
            else if (!destName.StartsWith("_"))
                Debug.LogWarning($"{messagePrefix} Destination {destName} does not start with underscore, which can be called over network RPC.\nIf this is intentional, you may safely ignore this warning.", currentScript);
        }

        static string ReplaceRegex(Match match) {
            switch (match.Groups[1].Value) {
                case "0": return @"(?:[1-9]\d*|0)";
                case "1": return @"\w+";
                default: return "";
            }
        }
    }
}
