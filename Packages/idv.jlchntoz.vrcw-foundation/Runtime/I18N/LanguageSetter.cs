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
    public partial class LanguageSetter : UdonSharpBehaviour {
        [SerializeField, HideInInspector, BindUdonSharpEvent] LanguageManager manager;
        [SerializeField, BindEvent(typeof(Toggle), nameof(Toggle.onValueChanged), nameof(_OnToggleClick))] GameObject entryTemplate;
        [SerializeField, Resolve(nameof(scrollRect) + "." + nameof(ScrollRect.content))] RectTransform parent;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField, HideInInspector, Resolve(nameof(scrollRect))] GameObject scrollRectObject;
        [SerializeField, HideInInspector, Resolve(nameof(scrollRect))] Transform scrollRectTransform;
        [SerializeField, BindEvent(nameof(Button.onClick), nameof(_OnDropdownButtonClick))] Button dropdownButton;
        [SerializeField, HideInInspector] RectTransform dropdownParent;
        Toggle[] spawnedEntries;
        RectTransform[] spawnedEntryRects;
        bool hasInit = false;
        bool afterFirstRun;
        int index = -1;

        void OnEnable() {
            if (afterFirstRun) {
                if (!Utilities.IsValid(dropdownButton)) _Scroll();
                return;
            }
            afterFirstRun = true;
            entryTemplate.SetActive(false);
            if (!Utilities.IsValid(parent)) parent = (RectTransform)transform;
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
            int count = keys.Length;
            spawnedEntries = new Toggle[count];
            spawnedEntryRects = new RectTransform[count];
            for (int i = 0; i < count; i++) {
                var entry = Instantiate(entryTemplate);
                entry.transform.SetParent(parent, false);
                var text = entry.GetComponentInChildren<Text>(true);
                if (Utilities.IsValid(text)) text.text = names[i];
                var tmp = entry.GetComponentInChildren<TextMeshProUGUI>(true);
                if (Utilities.IsValid(tmp)) tmp.text = names[i];
                entry.SetActive(true);
                spawnedEntries[i] = entry.GetComponent<Toggle>();
                spawnedEntryRects[i] = entry.GetComponent<RectTransform>();
            }
            hasInit = true;
            _OnLanguageChanged();
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _OnLanguageChanged() {
            if (!hasInit) return;
            index = Array.IndexOf(manager.LanguageKeys, manager.LanguageKey);
            for (int i = 0; i < spawnedEntries.Length; i++)
                spawnedEntries[i].SetIsOnWithoutNotify(i == index);
            if (!Utilities.IsValid(dropdownButton) || !Utilities.IsValid(scrollRectObject)) {
                _Scroll();
                return;
            }
            scrollRectObject.SetActive(false);
            if (Utilities.IsValid(dropdownParent)) scrollRectTransform.SetParent(transform, true);
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _Scroll() {
            if (!Utilities.IsValid(scrollRect)) return;
            var rectTransform = spawnedEntryRects[index];
            if (!Utilities.IsValid(rectTransform)) return;
            var viewPort = scrollRect.viewport;
            if (!Utilities.IsValid(viewPort)) return;
            var normalizedPosition = scrollRect.normalizedPosition;
            var pos = Rect.PointToNormalized(parent.rect, rectTransform.rect.center + (Vector2)rectTransform.localPosition);
            if (scrollRect.horizontal) normalizedPosition.x = pos.x;
            if (scrollRect.vertical) normalizedPosition.y = pos.y;
            scrollRect.normalizedPosition = normalizedPosition;
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

#if COMPILER_UDONSHARP
        public
#endif
        void _OnDropdownButtonClick() {
            if (!Utilities.IsValid(scrollRectObject)) return;
            if (scrollRectObject.activeSelf) {
                if (Utilities.IsValid(dropdownParent)) scrollRectTransform.SetParent(transform, true);
                scrollRectObject.SetActive(false);
                return;
            }
            if (Utilities.IsValid(dropdownParent)) scrollRectTransform.SetParent(dropdownParent, true);
            scrollRectObject.SetActive(true);
            SendCustomEventDelayedFrames(nameof(_Scroll), 3);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    partial class LanguageSetter : ISelfPreProcess {
        public int Priority => -1;

        public void PreProcess() {
            if (dropdownButton == null) return;
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas != null) dropdownParent = canvas.transform as RectTransform;
            if (scrollRect == null) return;
            if (scrollRectObject == null) scrollRectObject = scrollRect.gameObject;
            if (!scrollRectObject.TryGetComponent(out LayoutElement le))
                le = scrollRectObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }
    }
#endif
}