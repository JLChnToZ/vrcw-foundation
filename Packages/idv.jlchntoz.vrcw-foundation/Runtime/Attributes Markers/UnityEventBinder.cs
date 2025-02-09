using System;

namespace JLChnToZ.VRC.Foundation {
    /// <summary>
    /// Binds an event to <see cref="UdonSharp.UdonSharpBehaviour"/> on build.
    /// </summary>
    /// <remarks>
    /// There are several ways to use this attribute:
    /// <list type="bullet">
    /// <item>
    /// <term>Field</term>
    /// <description>
    /// You can bind the event in the component in the field.
    /// <example><code><![CDATA[
    /// [BindEvent(nameof(Button.onClick), nameof(_OnButtonClick)] Button button;
    /// public void _OnButtonClick() { ... }
    /// ]]></code></example>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Class</term>
    /// <description>
    /// You can bind the event in the component attached to the same game object.
    /// <example><code><![CDATA[
    /// [RequireComponent(typeof(Button))]
    /// [BindEvent(typeof(Button), nameof(Button.onClick), nameof(_OnButtonClick))]
    /// public class ButtonHandler : UdonSharpBehaviour {
    ///    public void _OnButtonClick() { ... }
    /// }
    /// ]]></code></example>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class BindEventAttribute : Attribute, IEquatable<BindEventAttribute> {
        /// <summary>
        /// The source event name. (That is the property name of the event)
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// The destination event name. (The name of a public method with no arguments)
        /// </summary>
        public string Destination { get; set; }
        /// <summary>
        /// The source type of the event.
        /// </summary>
        /// <remarks>
        /// If the component type is different from the field type, you must specify the source type.
        /// </remarks>
        public Type SourceType { get; set; }

        public BindEventAttribute(string source, string destination) {
            Source = source;
            Destination = destination;
        }

        public BindEventAttribute(Type sourceType, string source, string destination) : this(source, destination) {
            SourceType = sourceType;
        }

        public bool Equals(BindEventAttribute other) =>
            other != null &&
            Source == other.Source &&
            Destination == other.Destination &&
            SourceType == other.SourceType;

        public override bool Equals(object obj) => obj is BindEventAttribute other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Source, Destination, SourceType);
    }
}
