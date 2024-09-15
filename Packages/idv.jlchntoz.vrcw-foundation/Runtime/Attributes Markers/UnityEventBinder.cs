using System;

namespace JLChnToZ.VRC.Foundation {

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class BindEventAttribute : Attribute {
        public string Source { get; set; }
        public string Destination { get; set; }
        public Type SourceType { get; set; }

        public BindEventAttribute(string source, string destination) {
            Source = source;
            Destination = destination;
        }

        public BindEventAttribute(Type sourceType, string source, string destination) : this(source, destination) {
            SourceType = sourceType;
        }
    }
}
