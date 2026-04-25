#if !COMPILER_UDONSHARP
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    sealed class TextNode : BaseNode {
        readonly string text;

        public TextNode(string text) => this.text = text;

        public override void Write(StringBuilder sb) => sb.Append(text);

        public override bool Next() => false;
    }
}
#endif
