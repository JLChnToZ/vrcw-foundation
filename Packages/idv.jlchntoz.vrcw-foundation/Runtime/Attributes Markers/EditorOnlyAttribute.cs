using System;

namespace JLChnToZ.VRC.Foundation {
    [AttributeUsage(AttributeTargets.Class)]
    public class EditorOnlyAttribute : Attribute {
        public EditorOnlyAttribute() { }
    }
}