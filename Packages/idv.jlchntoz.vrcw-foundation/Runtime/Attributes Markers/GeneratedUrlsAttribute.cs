using System;

namespace JLChnToZ.VRC.Foundation {
    [AttributeUsage(AttributeTargets.Field)]
    public class GeneratedUrlsAttribute : Attribute {
        public string Pattern { get; set; }
        public string PatternSourceProperty { get; set; }
        public int Limit { get; set; } = int.MaxValue;

        public GeneratedUrlsAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class GeneratedUrlMapperAttribute : Attribute {
        public string RegexPattern { get; set; }
        public string RegexPatternSourceProperty { get; set; }
        public string TargetUrlArray { get; set; }

        public GeneratedUrlMapperAttribute() { }
    }
}
