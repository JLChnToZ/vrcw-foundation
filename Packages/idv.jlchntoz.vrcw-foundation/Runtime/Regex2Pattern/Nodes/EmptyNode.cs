#if !COMPILER_UDONSHARP
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    sealed class EmptyNode : BaseNode {
        public static readonly EmptyNode instance = new EmptyNode();

        EmptyNode() { }

        public override void Write(StringBuilder sb) { }

        public override bool Next() => false;
    }
}
#endif
