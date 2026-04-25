#if !COMPILER_UDONSHARP
using System.Collections.Generic;

namespace JLChnToZ.Regex2Pattern {
    class SequenceNode : NestedNode {
        public SequenceNode() { }

        SequenceNode(List<BaseNode> children) : base(children) { }

        public override BaseNode Clone() => new SequenceNode(children);
    }
}
#endif
