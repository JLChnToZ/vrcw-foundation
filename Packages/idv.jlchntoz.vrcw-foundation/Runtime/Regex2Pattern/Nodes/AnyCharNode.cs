#if !COMPILER_UDONSHARP
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    sealed class AnyCharNode : BaseNode {
        static char[] digitCharSet, wordCharSet;
        readonly char[] charSet;

        public static AnyCharNode Digit => new AnyCharNode(digitCharSet ??= Parser.DIGITS.ToCharArray());

        public static AnyCharNode Word => new AnyCharNode(wordCharSet ??= Parser.WORDS.ToCharArray());

        public AnyCharNode(char[] charSet) => this.charSet = charSet;

        public override void Write(StringBuilder sb) => sb.Append(charSet[index]);

        public override bool Next() {
            int nextIndex = index + 1;
            if (nextIndex >= charSet.Length) return false;
            index = nextIndex;
            return true;
        }

        public override BaseNode Clone() => new AnyCharNode(charSet);
    }
}
#endif
