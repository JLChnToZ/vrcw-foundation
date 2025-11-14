using System;
using System.Buffers;
using System.Collections;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        /// <summary>
        /// Resolve the path from the given object.
        /// </summary>
        /// <param name="from">The object to resolve from.</param>
        /// <returns>An enumerable object that will iterate through all resolved objects.</returns>
        public ResolveResults Resolve(object from) => new ResolveResults(this, from);

        /// <summary>
        /// Try to resolve the path from the given object.
        /// </summary>
        /// <param name="from">The object to resolve from.</param>
        /// <param name="result">The resolved object.</param>
        /// <returns><c>true</c> if the path is resolved successfully, otherwise <c>false</c>.</returns>
        public bool TryResolve(object from, out object result) => Resolve(from).TryOne(out result);

        /// <summary>
        /// An enumerable object that will iterate through all resolved objects.
        /// </summary>
        public readonly struct ResolveResults : IEnumerable {
            readonly Resolver resolver;
            readonly object from;

            public ResolveResults(Resolver resolver, object from) {
                this.from = from;
                this.resolver = resolver;
            }

            /// <summary>
            /// Try to get the first resolved object.
            /// </summary>
            /// <param name="result">The resolved object.</param>
            /// <returns><c>true</c> if the path is resolved successfully, otherwise <c>false</c>.</returns>
            public bool TryOne(out object result) {
                using (var results = GetEnumerator())
                    if (results.MoveNext()) {
                        result = results.Current;
                        return true;
                    }
                result = null;
                return false;
            }

            /// <summary>
            /// Get the enumerator to iterate through all resolved objects.
            /// </summary>
            /// <returns>The enumerator to iterate through all resolved objects.</returns>
            /// <remarks>
            /// Usually, you should use <c>foreach</c> instead of calling this method directly.
            /// </remarks>
            public ResolveResultsEnumerator GetEnumerator() => new ResolveResultsEnumerator(resolver, from);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// An enumerator to iterate through all resolved objects.
        /// </summary>
        public struct ResolveResultsEnumerator : IEnumerator, IDisposable {
            readonly IResolverCommand[] commands;
            readonly ICommandState[] states;
            readonly object from;
            readonly int count;
            int depth;

            /// <summary>
            /// Get the yielded resolved object.
            /// </summary>
            public object Current => states[count - 1].Current;

            public ResolveResultsEnumerator(Resolver resolver, object from) {
                if (resolver.commands == null)
                    resolver.commands = resolver.commandList.ToArray();
                this.from = from;
                commands = resolver.commands;
                count = commands.Length;
                states = ArrayPool<ICommandState>.Shared.Rent(count);
                for (int i = 0; i < count; i++)
                    states[i] = resolver.commands[i].CreateState();
                depth = 0;
                Reset();
            }

            /// <summary>
            /// Resolve the next object from current state.
            /// </summary>
            /// <returns><c>true</c> if the next object is resolved successfully, otherwise <c>false</c>.</returns>
            public bool MoveNext() {
                while (depth >= 0 && depth < count) {
                    var state = states[depth];
                    commands[depth].Next(state);
                    if (states[depth].Giveup) {
                        depth--;
                        continue;
                    }
                    if (depth >= count - 1) return true;
                    depth++;
                    commands[depth].Reset(states[depth], state.Current);
                }
                return false;
            }

            /// <summary>
            /// Reset the enumerator to the initial state.
            /// </summary>
            public void Reset() {
                depth = 0;
                if (commands.Length == 0) return;
                commands[0].Reset(states[0], from);
            }

            /// <summary>
            /// Dispose the enumerator. Release the resources.
            /// </summary>
            public void Dispose() {
                ArrayPool<ICommandState>.Shared.Return(states);
            }
        }
    }
}