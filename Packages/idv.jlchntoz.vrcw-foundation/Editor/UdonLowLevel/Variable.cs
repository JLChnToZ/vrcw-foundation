using System;

namespace JLChnToZ.VRC.Foundation.UdonLowLevel {
    public readonly struct VariableName : IEquatable<VariableName> {
        public readonly string key;

        public bool IsValid => !string.IsNullOrEmpty(key);
    
        public VariableName(string key) => this.key = key;

        public bool Equals(VariableName other) => string.Equals(key, other.key);

        public override bool Equals(object obj) => obj is VariableName other && Equals(other);

        public override int GetHashCode() => key?.GetHashCode() ?? 0;

        public override string ToString() => key ?? "";
    
        public static implicit operator VariableName(string key) => new VariableName(key);
    }

    [Flags]
    public enum VariableAttributes {
        None = 0x0,
        Public = 0x1,
        SyncNone = 0x2,
        SyncLinear = 0x4,
        SyncSmooth = 0x8,
        Sync = SyncNone | SyncLinear | SyncSmooth,
        DefaultThis = 0x10,
        Constant = 0x20,
    }

    public struct VariableDefinition {
        public VariableAttributes attributes;
        public Type type;
        public object value;

        public VariableDefinition(Type type = null, VariableAttributes attributes = VariableAttributes.None, object value = null) {
            this.type = type ?? value?.GetType() ?? typeof(object);
            this.attributes = attributes;
            this.value = value ?? (type != null && type != typeof(void) && type.IsValueType ? Activator.CreateInstance(type) : null);
        }
    }
}