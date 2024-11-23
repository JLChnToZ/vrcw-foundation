using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

namespace JLChnToZ.VRC.Foundation.I18N {
    /// <summary>
    /// A component allows user to select language from <see cref="LanguageManager"/>.
    /// </summary>
    /// <remarks>
    /// This component need to attach to an UI.
    /// Entry template must be a <see cref="Toggle"/> with a <see cref="Text"/> or <see cref="TextMeshProUGUI"/> component in it.
    /// </remarks>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("JLChnToZ VRCW Foundation/Locales/Language Setter")]
    [DefaultExecutionOrder(1)]
    public class LanguageSetter : UdonSharpBehaviour {
        [SerializeField, HideInInspector, BindUdonSharpEvent] LanguageManager manager;
        [SerializeField] GameObject entryTemplate;
        Toggle[] spawnedEntries;
        bool hasInit = false;
        bool afterFirstRun;

        void OnEnable() {
            if (afterFirstRun) return;
            afterFirstRun = true;
            entryTemplate.SetActive(false);
            SendCustomEventDelayedFrames(nameof(_DetectLanguageInit), 0);
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _DetectLanguageInit() {
            var keys = manager.LanguageKeys;
            var names = manager.LanguageNames;
            if (!Utilities.IsValid(keys) || !Utilities.IsValid(names)) {
                SendCustomEventDelayedFrames(nameof(_DetectLanguageInit), 0);
                return;
            }
            spawnedEntries = new Toggle[keys.Length];
            for (int i = 0; i < keys.Length; i++) {
                var entry = Instantiate(entryTemplate);
                entry.transform.SetParent(transform, false);
                var text = entry.GetComponentInChildren<Text>(true);
                if (Utilities.IsValid(text)) text.text = names[i];
                var tmp = entry.GetComponentInChildren<TextMeshProUGUI>(true);
                if (Utilities.IsValid(tmp)) tmp.text = names[i];
                entry.SetActive(true);
                spawnedEntries[i] = entry.GetComponent<Toggle>();
            }
            hasInit = true;
            _OnLanguageChanged();
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _OnLanguageChanged() {
            if (!hasInit) return;
            int index = Array.IndexOf(manager.LanguageKeys, manager.LanguageKey);
            for (int i = 0; i < spawnedEntries.Length; i++)
                spawnedEntries[i].SetIsOnWithoutNotify(i == index);
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _OnToggleClick() {
            if (!hasInit) return;
            for (int i = 0; i < spawnedEntries.Length; i++) {
                if (spawnedEntries[i].isOn) {
                    manager.LanguageKey = manager.LanguageKeys[i];
                    break;
                }
            }
        }
    }
}