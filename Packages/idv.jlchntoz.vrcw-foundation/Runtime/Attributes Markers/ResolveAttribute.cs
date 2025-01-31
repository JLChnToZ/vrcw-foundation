using System;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Mark a field to be auto resolved from a path on build time.
    /// </summary>
    /// <remarks>
    /// When multiple <see cref="ResolveAttribute"/> are used on the same field,
    /// it will recursively resolve the field from the path until it reaches the last one.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ResolveAttribute : PropertyAttribute {
        /// <summary>
        /// The path to resolve the field from.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// The type of the source object.
        /// It is required when multiple <see cref="ResolveAttribute"/> are used on the same field.
        /// </summary>
        public Type SourceType { get; set; }

        /// <summary>
        /// Should ignore the field if it is already assigned.
        /// Default is <c>true</c>.
        /// </summary>
        public bool NullOnly { get; set; }

        /// <summary>
        /// Should hide the field in the inspector if it is resolvable,
        /// that is, the source object is found.
        /// </summary>
        public bool HideInInspectorIfResolvable { get; set; }

        /// <summary>
        /// Mark a field to be auto resolved from a path on build time.
        /// </summary>
        /// <param name="source">The path to resolve the field from.</param>
        /// <remarks>
        /// It supports glob-like path. For example, `/Some Root/**/Some Object#someProp` will resolve
        /// the `someProp` from the first object named `Some Object` in the hierarchy under `Some Root`.
        /// </remarks>
        public ResolveAttribute(string source) {
            Source = source;
            NullOnly = true;
        }
    }
}
