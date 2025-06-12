using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this attribute to an <see cref="int"/> field to display it as a toggle and a number field in the inspector.
    /// If the toggle is off, the number field will be disabled and display a custom text.
    /// </summary>
    /// <remarks>
    /// This is for integer fields that can be toggled on or off, where the value is non-negative when toggled on and -1 when toggled off.
    /// </remarks>
    public class ToggleAndNumberFieldAttribute : PropertyAttribute {
        /// <summary>
        /// The text to display in the number field when the toggle is off.
        /// If not set, it defaults to an empty string.
        /// </summary>
        public string DisabledText { get; set; }
        public ToggleAndNumberFieldAttribute() { }
    }
}