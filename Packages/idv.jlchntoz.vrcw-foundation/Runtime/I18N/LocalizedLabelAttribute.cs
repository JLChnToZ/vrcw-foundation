using UnityEngine;

namespace JLChnToZ.VRC.Foundation.I18N {
    /// <summary>
    /// Attach this attribute to a field to make it a localized label.
    /// </summary>
    public class LocalizedLabelAttribute : PropertyAttribute {
        /// <summary>
        /// The key of the localized label.
        /// </summary>
        /// <remarks>
        /// If not provided, the namespace, type name and field name will be used as key.
        /// </remarks>
        public string Key { get; set; }
        /// <summary>
        /// The key of the localized tooltip.
        /// </summary>
        /// <remarks>
        /// If not provided, it will defaults to <c>{Key}:tooltip</c>.
        /// </remarks>
        public string TooltipKey { get; set; }

        public LocalizedLabelAttribute() { }
    }

    /// <summary>
    /// Attach this attribute to an enum field to make it a localized enum.
    /// </summary>
    public class LocalizedEnumAttribute : PropertyAttribute {
        /// <summary>
        /// The key of the localized enum label.
        /// </summary>
        /// <remarks>
        /// If not provided, the enum type name will be used as key.
        /// </remarks>
        public string Key { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="LocalizedEnumAttribute"/>.
        /// </summary>
        public LocalizedEnumAttribute() { }
    }

    /// <summary>
    /// Attach a header above the field with localized text.
    /// </summary>
    public class LocalizedHeaderAttribute : PropertyAttribute {
        public string Key { get; private set; }

        /// <summary>
        /// Create a new instance of <see cref="LocalizedHeaderAttribute"/>.
        /// </summary>
        /// <param name="key">The key of the localized header.</param>
        public LocalizedHeaderAttribute(string key) {
            Key = key;
        }
    }
}