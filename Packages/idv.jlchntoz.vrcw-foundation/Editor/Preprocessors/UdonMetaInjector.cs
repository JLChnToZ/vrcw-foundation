using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    internal sealed class UdonMetaInjector : UdonSharpPreProcessor {
        VRC_SceneDescriptor worldDescriptor = null;
        DateTime buildTime;

        public override void OnPreprocess(Scene scene) {
            buildTime = DateTime.UtcNow;
            worldDescriptor = VRC_SceneDescriptor.Instance;
            base.OnPreprocess(scene);
        }

        protected override void ProcessEntry(Type type, MonoBehaviour entry, UdonBehaviour udon) {
            bool changed = false;
            foreach (var field in GetFields<UdonMetaAttribute>(type)) {
                try {
                    var attr = field.GetCustomAttribute<UdonMetaAttribute>(true);
                    if (attr == null) continue;
                    var metaType = attr.Type;
                    var fieldType = field.FieldType;
                    switch (metaType) {
                        case UdonMetaAttributeType.NetworkID:
                            if (worldDescriptor.NetworkIDLookup.TryGetValue(entry.gameObject, out var nwPair)) {
                                field.SetValue(entry, Convert.ChangeType(nwPair.ID, fieldType));
                                changed = true;
                            }
                            break;
                        case UdonMetaAttributeType.NetworkSyncModeNone:
                        case UdonMetaAttributeType.NetworkSyncModeContinuous:
                        case UdonMetaAttributeType.NetworkSyncModeManual:
                            var syncType = udon.SyncMethod;
                            bool isEqual = false;
                            switch (metaType) {
                                case UdonMetaAttributeType.NetworkSyncModeNone:
                                    isEqual = syncType == Networking.SyncType.None;
                                    break;
                                case UdonMetaAttributeType.NetworkSyncModeContinuous:
                                    isEqual = syncType == Networking.SyncType.Continuous;
                                    break;
                                case UdonMetaAttributeType.NetworkSyncModeManual:
                                    isEqual = syncType == Networking.SyncType.Manual;
                                    break;
                            }
                            field.SetValue(entry, Convert.ChangeType(isEqual, fieldType));
                            changed = true;
                            break;
                        case UdonMetaAttributeType.NetworkSyncMode:
                            field.SetValue(entry, Convert.ChangeType(udon.SyncMethod, fieldType));
                            break;
                        case UdonMetaAttributeType.BuiltTimeStamp:
                            field.SetValue(entry, Convert.ChangeType(buildTime, fieldType));
                            changed = true;
                            break;
                        case UdonMetaAttributeType.WorldBlueprintID:
                            if (worldDescriptor.TryGetComponent(out PipelineManager pipelineManager)) {
                                field.SetValue(entry, Convert.ChangeType(pipelineManager.blueprintId, fieldType));
                                changed = true;
                            }
                            break;
                    }
                } catch (Exception e) {
                    Debug.LogError($"[{GetType().Name}] `{entry.name}` is not correctly configured.", entry);
                    Debug.LogException(e, entry);
                }
            }
            if (changed && entry is UdonSharpBehaviour usharp) UdonSharpEditorUtility.CopyProxyToUdon(usharp);
        }
    }
}