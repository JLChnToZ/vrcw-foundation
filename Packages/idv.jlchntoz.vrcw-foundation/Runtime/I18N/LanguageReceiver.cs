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
    public class LanguageReceiver : UdonSharpBehaviour {
        [SerializeField, HideInInspector, BindUdonSharpEvent] LanguageManager manager;
        [SerializeField, LocalizedLabel] string key;
        object[] args;
        Text text;
        TMP_Text textMeshPro;
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
            text = GetComponent<Text>();
            textMeshPro = GetComponent<TextMeshProUGUI>();
            if (!Utilities.IsValid(textMeshPro)) textMeshPro = GetComponent<TextMeshPro>();
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
}