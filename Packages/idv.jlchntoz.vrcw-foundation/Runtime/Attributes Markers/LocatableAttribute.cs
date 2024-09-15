using System;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Appends a resolve button to the field in inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocatableAttribute : PropertyAttribute {
        /// <summary>
        /// The type names that this field can resolve to.
        /// </summary>
        public string[] TypeNames { get; set; }

        /// <summary>
        /// The path of the prefab to instantiate.
        /// </summary>
        /// <remarks>
        /// It will try to instantiate the prefab at the path if no matching object found.
        /// </remarks>
        public string InstaniatePrefabPath { get; set; }

        /// <summary>
        /// The GUID of the prefab to instantiate.
        /// </summary>
        /// <remarks>
        /// It will try to instantiate the prefab with the GUID if no matching object found.
        /// </remarks>
        public string InstaniatePrefabGuid { get; set; }

        /// <summary>
        /// Where of the hierarchy to instantiate the prefab.
        /// </summary>
        public InstaniatePrefabHierachyPosition InstaniatePrefabPosition { get; set; }

        /// <summary>
        /// Should resolve the field when building the project. Currently is not yet implemented.
        /// </summary>
        public bool ResolveOnBuild { get; set; }

        public LocatableAttribute() { }

        public LocatableAttribute(params string[] typeNames) {
            TypeNames = typeNames;
        }

        /// <summary>
        /// The position of the prefab to instantiate.
        /// </summary>
        [Flags]
        public enum InstaniatePrefabHierachyPosition : byte {
            /// <summary>At the root of the hierarchy.</summary>
            Root = 0,
            /// <summary>As a child of the attached game object.</summary>
            Child = 1,
            /// <summary>At the same level of the attached game object.</summary>
            SameLevel = 2,
            /// <summary>As the previous sibling.</summary>
            PreviousSibling = 4,
            /// <summary>As the next sibling.</summary>
            NextSibling = 8,
            /// <summary>As the first of the designated level.</summary>
            First = 16,
            /// <summary>As the last of the designated level.</summary>
            Last = 32,

            /// <summary>As the first child of the root.</summary>
            FirstRoot = First | Root,
            /// <summary>As the last child of the root.</summary>
            LastRoot = Last | Root,
            /// <summary>As the first child of the attached game object.</summary>
            FirstChild = First | Child,
            /// <summary>As the last child of the attached game object.</summary>
            LastChild = Last | Child,
            /// <summary>As the first child of the same level.</summary>
            FirstSameLevel = First | SameLevel,
            /// <summary>As the last child of the same level.</summary>
            LastSameLevel = Last | SameLevel,
            /// <summary>Just before the attached game object.</summary>
            Before = PreviousSibling | SameLevel,
            /// <summary>Just after the attached game object.</summary>
            After = NextSibling | SameLevel,
        }
    }
}
