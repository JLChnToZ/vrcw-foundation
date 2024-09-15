using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        TextMeshProUGUI textMeshPro;
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
            if (manager == null) return;
            text = GetComponent<Text>();
            textMeshPro = GetComponent<TextMeshProUGUI>();
            if (string.IsNullOrEmpty(key)) {
                if (text != null) key = text.text;
                else if (textMeshPro != null) key = textMeshPro.text;
            }
            _OnLanguageChanged();
        }

        public void _OnLanguageChanged() {
            var result = manager.GetLocale(key);
            if (args != null && args.Length > 0)
                result = string.Format(result, args);
            if (text != null) text.text = result;
            if (textMeshPro != null) textMeshPro.text = result;
        }
    }
}