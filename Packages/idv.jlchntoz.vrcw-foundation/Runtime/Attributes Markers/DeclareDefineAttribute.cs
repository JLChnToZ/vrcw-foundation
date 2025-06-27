using System;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this to an assembly to declare a define.
    /// </summary>
    /// <remarks>
    /// This is the alternative to using "versionDefines" in manifest.json, since that don't work with UdonSharp compiler.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class DeclareDefineAttribute : Attribute {
        /// <summary>
        /// The name of the define to declare.
        /// </summary>
        public string DefineName { get; }

        public DeclareDefineAttribute(string defineName) {
            DefineName = defineName;
        }
    }
}