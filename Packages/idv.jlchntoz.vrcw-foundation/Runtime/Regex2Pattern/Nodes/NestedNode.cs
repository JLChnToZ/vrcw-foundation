#if !COMPILER_UDONSHARP
using System.Collections.Generic;
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    abstract class NestedNode : BaseNode {
        protected readonly List<BaseNode> children = new List<BaseNode>();

        protected NestedNode() { }

        protected NestedNode(List<BaseNode> children) {
            var childrenCount = children.Count;
            if (this.children.Capacity < childrenCount) this.children.Capacity = childrenCount;
            foreach (var child in children)
                this.children.Add(child.Clone());
        }

        public void Add(BaseNode node) {
            children.Add(node);
        }

        public override void Write(StringBuilder sb) {
            foreach (var child in children) child.Write(sb);
        }

        public override bool Next() {
            foreach (var child in children) {
                if (child.Next()) return true;
                child.Reset();
            }
            return false;
        }

        public override void Reset() {
            foreach (var child in children) child.Reset();
            base.Reset();
        }
    }
}
#endif
