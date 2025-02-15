using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

namespace JLChnToZ.VRC.Foundation.I18N {
    /// <summary>
    /// A component receives language data from <see cref="LanguageManager"/>
    /// and set the text of the attached <see cref="Text"/> or <see cref="TextMeshProUGUI"/> component.
    /// </summary>
    [TMProMigratable]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [AddComponentMenu("JLChnToZ VRCW Foundation/Locales/Language Receiver")]
    [DefaultExecutionOrder(1)]
    public partial class LanguageReceiver : UdonSharpBehaviour {
        [SerializeField, HideInInspector, BindUdonSharpEvent] LanguageManager manager;
        [SerializeField, LocalizedLabel] string key;
        object[] args;
        [SerializeField, HideInInspector, Resolve(".", NullOnly = false)] Text text;
        [SerializeField, HideInInspector, Resolve(".", NullOnly = false)] TMP_Text textMeshPro;
        bool afterFirstRun;

        /// <summary>
        /// Additonal arguments for the language string.
        /// </summary>
        public object[] Args {
            get => args;
            set {
                args = value;
                _OnLanguageChanged();
            }
        }

        void OnEnable() {
            if (afterFirstRun) return;
            afterFirstRun = true;
            if (!Utilities.IsValid(manager)) return;
            if (string.IsNullOrEmpty(key)) {
                if (Utilities.IsValid(text)) key = text.text;
                else if (Utilities.IsValid(textMeshPro)) key = textMeshPro.text;
            }
            _OnLanguageChanged();
        }

#if COMPILER_UDONSHARP
        public
#endif
        void _OnLanguageChanged() {
            var result = manager.GetLocale(key);
            if (Utilities.IsValid(args) && args.Length > 0)
                result = string.Format(result, args);
            if (Utilities.IsValid(text)) text.text = result;
            if (Utilities.IsValid(textMeshPro)) textMeshPro.text = result;
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public partial class LanguageReceiver : ISelfPreProcess {
        public int Priority => 0;

        public void PreProcess() {
            if (TryGetComponent(out Text text)) {
                if (string.IsNullOrEmpty(key)) key = text.text;
                text.text = "";
            }
            if (TryGetComponent(out TMP_Text tmpro)) {
                if (string.IsNullOrEmpty(key)) key = tmpro.text;
                tmpro.text = "";
            }
        }
    }
#endif
}