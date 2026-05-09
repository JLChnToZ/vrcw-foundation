using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.SDK3.Data;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class BindUdonSharpEventPreprocessor : UdonSharpPreProcessor {
        readonly Dictionary<UdonSharpEventSender, HashSet<UnityObject>> eventSenders =
            new Dictionary<UdonSharpEventSender, HashSet<UnityObject>>();
        readonly Dictionary<UdonSharpEventSender, HashSet<(string eventName, UnityObject target)>> eventSenderWithEventNames =
            new Dictionary<UdonSharpEventSender, HashSet<(string, UnityObject)>>();

        protected override void ProcessEntry(Type type, MonoBehaviour dest, UdonBehaviour udon) {
            var fieldInfos = GetFields<BindUdonSharpEventAttribute>(type);
            foreach (var field in fieldInfos) {
                var targetObj = field.GetValue(dest);
                var attr = field.GetCustomAttribute<BindUdonSharpEventAttribute>(true);
                if (targetObj is Array array)
                    for (int i = 0, length = array.GetLength(0); i < length; i++)
                        AddEntry(array.GetValue(i) as UnityObject, dest, attr.bindEventNames);
                else if (targetObj is UnityObject unityObject)
                    AddEntry(unityObject, dest, attr.bindEventNames);
            }
        }

        public override void OnPreprocess(Scene scene) {
            base.OnPreprocess(scene);
            foreach (var kv in eventSenders) {
                var sender = kv.Key;
                if (sender == null) {
                    Debug.LogError("[BindUdonSharpEventPreprocessor] Event sender is null, this should not happen.", sender);
                    continue;
                }
                using (HashSetPool<UnityObject>.Get(out var remapped))
                using (var so = new SerializedObject(sender)) {
                    var prop = so.FindProperty("targets");
                    for (int i = 0, count = prop.arraySize; i < count; i++)
                        if (prop.GetArrayElementAtIndex(i).objectReferenceValue is UdonSharpBehaviour ub && ub != null)
                            remapped.Add(ub);
                    remapped.UnionWith(kv.Value);
                    prop.arraySize = remapped.Count;
                    {
                        int i = 0;
                        foreach (var entry in remapped)
                            prop.GetArrayElementAtIndex(i++).objectReferenceValue = entry;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            eventSenders.Clear();
            foreach (var kv in eventSenderWithEventNames) {
                var sender = kv.Key;
                if (sender == null) {
                    Debug.LogError("[BindUdonSharpEventPreprocessor] Event sender is null, this should not happen.", sender);
                    continue;
                }
                var eventTargetMap = sender.namedTargets ??= new DataDictionary();
                foreach (var (eventName, target) in kv.Value) {
                    if (string.IsNullOrEmpty(eventName)) continue;
                    var realTarget = target is UdonBehaviour ub ? ub :
                        target is UdonSharpBehaviour usb ? UdonSharpEditorUtility.GetBackingUdonBehaviour(usb) :
                        null;
                    if (realTarget == null) continue;
                    DataToken eventNameToken = BindEventPreprocessor.GetMappedName(eventName);
                    if (!eventTargetMap.TryGetValue(eventNameToken, out var dt)) {
                        eventTargetMap[eventNameToken] = realTarget;
                        continue;
                    }
                    DataList dtList;
                    switch (dt.TokenType) {
                        case TokenType.Reference:
                            if (dt == realTarget) break;
                            eventTargetMap[eventNameToken] = new DataList { dt, realTarget };
                            break;
                        case TokenType.DataList:
                            dtList = dt.DataList;
                            if (dtList.Contains(realTarget)) break;
                            dtList.Add(realTarget);
                            break;
                    }
                }
            }
            using (HashSetPool<UdonSharpEventSender>.Get(out var proceedSenders)) {
                proceedSenders.UnionWith(eventSenders.Keys);
                proceedSenders.UnionWith(eventSenderWithEventNames.Keys);
                foreach (var sender in proceedSenders)
                    UdonSharpEditorUtility.CopyProxyToUdon(sender);
            }
        }

        void AddEntry(UnityObject targetObj, UnityObject dest, string[] eventNames) {
            if (targetObj == null) return;
            if (targetObj is GameObject go) targetObj = go.GetComponent<UdonSharpEventSender>();
            else if (targetObj is UdonBehaviour ub) targetObj = UdonSharpEditorUtility.GetProxyBehaviour(ub);
            if (!(targetObj is UdonSharpEventSender sender) || sender == null) return;
            if (eventNames != null && eventNames.Length > 0) {
                if (!eventSenderWithEventNames.TryGetValue(sender, out var list))
                    eventSenderWithEventNames[sender] = list = new HashSet<(string, UnityObject)>();
                foreach (var eventName in eventNames)
                    list.Add((eventName, dest));
            } else {
                if (!eventSenders.TryGetValue(sender, out var list))
                    eventSenders[sender] = list = new HashSet<UnityObject>();
                list.Add(dest);
            }
        }
    }
}