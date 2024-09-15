using System;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Attach this to a <see cref="UnityEngine.MonoBehaviour"/> based class to mark it as editor only.
    /// </summary>
    /// <remarks>
    /// Any instances of the component will be removed when building the project.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorOnlyAttribute : Attribute {
        public EditorOnlyAttribute() { }
    }
}