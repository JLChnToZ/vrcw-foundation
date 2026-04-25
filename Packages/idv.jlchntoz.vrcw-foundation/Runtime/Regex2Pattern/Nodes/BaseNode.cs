#if !COMPILER_UDONSHARP
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JLChnToZ.Regex2Pattern {
    abstract class BaseNode : IGenerator {
        protected int index;

        public abstract void Write(StringBuilder sb);

        public abstract bool Next();

        public virtual void Reset() => index = 0;

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual BaseNode Clone() => this;
    }
}
#endif
