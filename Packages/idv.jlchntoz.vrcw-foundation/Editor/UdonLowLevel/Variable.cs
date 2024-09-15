using System;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    /// <summary>
    /// A variable name for use in Udon Assembly Builder.
    /// </summary>
    public readonly struct VariableName : IEquatable<VariableName> {
        /// <summary>The key of the variable.</summary>
        public readonly string key;

        /// <summary>Whether the variable name is valid.</summary>
        public bool IsValid => !string.IsNullOrEmpty(key);
    
        public VariableName(string key) => this.key = key;

        public bool Equals(VariableName other) => string.Equals(key, other.key);

        public override bool Equals(object obj) => obj is VariableName other && Equals(other);

        public override int GetHashCode() => key?.GetHashCode() ?? 0;

        public override string ToString() => key ?? "";
    
        public static implicit operator VariableName(string key) => new VariableName(key);
    }

    /// <summary>
    /// Attributes for a variable.
    /// </summary>
    [Flags]
    public enum VariableAttributes {
        /// <summary>No attributes.</summary>
        None = 0x0,
        /// <summary>The variable is public accessible.</summary>
        Public = 0x1,
        /// <summary>The variable is synchronized with the network without interpolation.</summary>
        SyncNone = 0x2,
        /// <summary>The variable is synchronized with the network with linear interpolation.</summary>
        SyncLinear = 0x4,
        /// <summary>The variable is synchronized with the network with smooth interpolation.</summary>
        SyncSmooth = 0x8,
        /// <summary>The variable is synchronized with the network regardless of the interpolation method.</summary>
        Sync = SyncNone | SyncLinear | SyncSmooth,
        /// <summary>This variable defaults to <c>this</c> execution context.</summary>
        DefaultThis = 0x10,
        /// <summary>This variable stores a constant value.</summary>
        Constant = 0x20,
    }

    /// <summary>
    /// A definition for a variable.
    /// </summary>
    public struct VariableDefinition {
        /// <summary>The attributes of the variable.</summary>
        public VariableAttributes attributes;
        /// <summary>The type of the variable.</summary>
        public Type type;
        /// <summary>The value of the variable.</summary>
        public object value;

        /// <summary>
        /// Create a new variable definition.
        /// </summary>
        /// <param name="type">The type of the variable.</param>
        /// <param name="attributes">The attributes of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public VariableDefinition(Type type = null, VariableAttributes attributes = VariableAttributes.None, object value = null) {
            this.type = type ?? value?.GetType() ?? typeof(object);
            this.attributes = attributes;
            this.value = value ?? (type != null && type != typeof(void) && type.IsValueType ? Activator.CreateInstance(type) : null);
        }
    }
}