#if !COMPILER_UDONSHARP
using System.Collections.Generic;
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    sealed class AnyNodeNode : NestedNode {
        public AnyNodeNode() { }

        AnyNodeNode(List<BaseNode> children) : base(children) { }

        public override void Write(StringBuilder sb) {
            if (index >= children.Count) return;
            children[index].Write(sb);
        }

        public override bool Next() {
            if (index >= children.Count) return false;
            if (!children[index].Next()) {
                int nextIndex = index + 1;
                if (nextIndex >= children.Count) return false;
                index = nextIndex;
            }
            return true;
        }

        public override BaseNode Clone() => new AnyNodeNode(children);
    }
}
#endif
