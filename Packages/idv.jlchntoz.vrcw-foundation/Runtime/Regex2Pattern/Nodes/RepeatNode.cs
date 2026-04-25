#if !COMPILER_UDONSHARP
namespace JLChnToZ.Regex2Pattern {
    sealed class RepeatNode : NestedNode {
        readonly BaseNode child;
        readonly int min, max;

        public RepeatNode(BaseNode child)
            : this(child, 0, 1) { }

        public RepeatNode(BaseNode child, int count)
            : this(child, count, count) { }

        public RepeatNode(BaseNode child, int min, int max) {
            this.child = child;
            this.min = min;
            this.max = max;
            Reset();
        }

        public override bool Next() {
            if (base.Next()) return true;
            if (children.Count >= max) return false;
            children.Add(child.Clone());
            return true;
        }

        public override void Reset() {
            children.Clear();
            base.Reset();
            for (int i = 0; i < min; i++)
                children.Add(child.Clone());
        }

        public override BaseNode Clone() => new RepeatNode(child, min, max);
    }
}
#endif
