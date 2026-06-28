using System;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this to an assembly to declare a define.
    /// </summary>
    /// <remarks>
    /// This is the alternative to using "versionDefines" in manifest.json, since that don't work with UdonSharp compiler.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DeclareDefineAttribute : DeclareDefineBaseAttribute {
        public DeclareDefineAttribute(string defineName) : base(defineName) {
        }
    }

    /// <summary>
    /// Attach this to an assembly to remove a define.
    /// This is for handling cases where a dependency has been removed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DeclareUndefineAttribute : DeclareDefineBaseAttribute {
        public DeclareUndefineAttribute(string defineName) : base(defineName) {
        }
    }

    public abstract class DeclareDefineBaseAttribute : Attribute {
        /// <summary>
        /// The name of the define to declare or remove.
        /// </summary>
        public string DefineName { get; }

        protected DeclareDefineBaseAttribute(string defineName) {
            DefineName = defineName;
        }
    }
}