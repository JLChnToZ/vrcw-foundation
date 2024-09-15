using System;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this attribute to a <see cref="UnityEngine.UI.Text"/> field to indicate that it should be migrated to <see cref="TMPro.TextMeshProUGUI"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = true)]
    public class TMProMigratableAttribute : Attribute {
        /// <summary>
        /// The field name of the <see cref="TMPro.TextMeshProUGUI"/> that the field should be migrated to.
        /// </summary>
        public string TMProFieldName { get; private set; }

        public TMProMigratableAttribute() : this("") { }

        public TMProMigratableAttribute(string tmProFieldName) {
            TMProFieldName = tmProFieldName;
        }
    }
}