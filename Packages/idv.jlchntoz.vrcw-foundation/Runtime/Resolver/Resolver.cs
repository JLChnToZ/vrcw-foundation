using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    /// <summary>
    /// A generic resolver to resolve object from a given path.
    /// </summary>
    /// <remarks>
    /// This resolver supports both property and hierarchy resolution.
    /// You may use glob-like pattern
    /// (<c>/</c> for path entry separation,
    /// <c>..</c> for parent,
    /// <c>..*</c> for any parent,
    /// <c>*</c> for wildcard,
    /// <c>**</c> for any children)
    /// to resolve in hierarchy,
    /// and use dot (<c>.</c>) to resolve properties.
    /// To combine-resolve through properties and hierarchy, you may use <c>#</c> to separate them.
    /// By default it will treat the pattern is property goes first if no path separator is found.
    /// </remarks>
    public class Resolver : IEquatable<Resolver> {
        static readonly Dictionary<(string, Type), IResolverCommand[]> cache = new Dictionary<(string, Type), IResolverCommand[]>();
        static readonly HashSet<IResolverCommand[]> iteratingCommands = new HashSet<IResolverCommand[]>();
        static readonly List<TryResolveMemberDelegate> customResolvers = new List<TryResolveMemberDelegate>();
        readonly string path;
        readonly Type destinationType;
        IResolverCommand[] commands;

        /// <summary>
        /// Register a custom resolve provider.
        /// </summary>
        public static event TryResolveMemberDelegate CustomResolveProvider {
            add => customResolvers.Add(value);
            remove => customResolvers.Remove(value);
        }

#if UDON
        static Resolver() {
            customResolvers.Add(TryResolveUdon);
        }

        static bool TryResolveUdon(object current, string propertyName, MemberInfo member, out object result) {
            if (current is global::VRC.Udon.UdonBehaviour udon && udon.TryGetProgramVariable(propertyName, out result))
                return true;
            result = null;
            return false;
        }
#endif

        public Resolver(string path, Type destinationType = null) {
            this.path = path;
            this.destinationType = destinationType;
        }

        void ParsePath() {
            if (string.IsNullOrWhiteSpace(path)) {
                commands = Array.Empty<IResolverCommand>();
                return;
            }
            if (cache.TryGetValue((path, destinationType), out commands)) return;
            var sb = new StringBuilder(path.Length);
            var commandList = new List<IResolverCommand>();
            var mode = ParsePathMode.Undertermined;
            int lastPoundIndex = path.LastIndexOf('#');
            if (lastPoundIndex < 0 && destinationType != null)
                commandList.Add(new SpecifyTypeCommand(destinationType));
            for (int i = 0, count = path.Length; i < count; i++) {
                switch (path[i]) {
                    case '#':
                        if (sb.Length > 0) mode = ParseSegment(mode, sb, commandList);
                        mode = mode == ParsePathMode.Hierarchy ? ParsePathMode.Property : ParsePathMode.Hierarchy;
                        break;
                    case '/':
                        switch (mode) {
                            case ParsePathMode.Undertermined:
                                if (sb.Length == 0) {
                                    commandList.Add(new HierarchyRootCommand());
                                    mode = ParsePathMode.Hierarchy;
                                    break;
                                }
                                goto case ParsePathMode.Hierarchy;
                            case ParsePathMode.Hierarchy:
                                mode = ParseSegment(mode, sb, commandList);
                                break;
                        }
                        break;
                    case '*':
                        if (mode == ParsePathMode.Undertermined) mode = ParsePathMode.Hierarchy;
                        goto default;
                    default:
                        sb.Append(path[i]);
                        break;
                }
                if (lastPoundIndex == i && destinationType != null)
                    commandList.Add(new SpecifyTypeCommand(destinationType));
            }
            if (sb.Length > 0) {
                if (mode != ParsePathMode.Hierarchy) mode = ParsePathMode.Property;
                ParseSegment(mode, sb, commandList);
            }
            commands = commandList.ToArray();
            cache[(path, destinationType)] = commands;
        }

        static ParsePathMode ParseSegment(ParsePathMode currentMode, StringBuilder sb, IList<IResolverCommand> commandList) {
            if (sb.Length == 0) {
                commandList.Add(new SelfCommand());
                return currentMode;
            }
            var pathSeg = sb.ToString();
            sb.Clear();
            switch (pathSeg) {
                case ".":
                case "this":
                    if (currentMode != ParsePathMode.Hierarchy) {
                        commandList.Add(new SelfCommand());
                        return ParsePathMode.Property;
                    }
                    goto default;
                case "..":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(new HierarchyParentCommand(false));
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "..*":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(new HierarchyParentCommand(true));
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "*":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(new HierarchyAnyChildCommand(false));
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "**":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(new HierarchyAnyChildCommand(true));
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                default:
                    switch (currentMode) {
                        case ParsePathMode.Hierarchy:
                            commandList.Add(new HierarchyChildCommand(pathSeg));
                            return ParsePathMode.Hierarchy;
                        case ParsePathMode.Property:
                        case ParsePathMode.Undertermined:
                            if (pathSeg.Contains("."))
                                foreach (var part in pathSeg.Split('.'))
                                    commandList.Add(new PropertyCommand(part));
                            else
                                commandList.Add(new PropertyCommand(pathSeg));
                            return ParsePathMode.Property;
                    }
                    return currentMode;
            }
        }

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

        public override string ToString() =>
            destinationType == null ? path : $"{path} <{destinationType.Name}>";

        public bool Equals(Resolver other) {
            if (null == other) return false;
            if (ReferenceEquals(this, other)) return true;
            return path == other.path && destinationType == other.destinationType;
        }

        public override bool Equals(object obj) => obj is Resolver other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(path, destinationType);

        /// <summary>
        /// Custom resolve delegate.
        /// </summary>
        /// <param name="from">The object to resolve from.</param>
        /// <param name="memberName">The member name to resolve.</param>
        /// <param name="member">The member info to resolve.</param>
        /// <param name="result">The resolved object.</param>
        /// <returns><c>true</c> if the member is resolved successfully, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// It will fallback to the default resolve behavior if all custom resolve delegates return <c>false</c>.
        /// </remarks>
        public delegate bool TryResolveMemberDelegate(object from, string memberName, MemberInfo member, out object result);

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
            readonly object[] states;
            readonly object from;
            readonly int count;
            readonly bool isClonned;
            int index;

            /// <summary>
            /// Get the yielded resolved object.
            /// </summary>
            public object Current => states[count];

            public ResolveResultsEnumerator(Resolver resolver, object from) {
                if (resolver.commands == null) resolver.ParsePath();
                this.from = from;
                count = resolver.commands.Length;
                if (count == 0 || iteratingCommands.Add(resolver.commands)) {
                    commands = resolver.commands;
                    isClonned = false;
                } else {
                    commands = ArrayPool<IResolverCommand>.Shared.Rent(count);
                    for (int i = 0; i < count; i++)
                        commands[i] = (IResolverCommand)resolver.commands[i].Clone();
                    iteratingCommands.Add(commands);
                    isClonned = true;
                }
                states = ArrayPool<object>.Shared.Rent(count + 1);
                index = 0;
                Reset();
            }

            /// <summary>
            /// Resolve the next object from current state.
            /// </summary>
            /// <returns><c>true</c> if the next object is resolved successfully, otherwise <c>false</c>.</returns>
            public bool MoveNext() {
                while (index >= 0 && index < count)
                    if (commands[index].Next(out var result)) {
                        index++;
                        states[index] = result;
                        if (index >= count) return true;
                        commands[index].Reset(result);
                    } else index--;
                return false;
            }

            /// <summary>
            /// Reset the enumerator to the initial state.
            /// </summary>
            public void Reset() {
                index = 0;
                commands[0].Reset(from);
                states[0] = from;
            }

            /// <summary>
            /// Dispose the enumerator. Release the resources.
            /// </summary>
            public void Dispose() {
                ArrayPool<object>.Shared.Return(states);
                iteratingCommands.Remove(commands);
                if (isClonned) ArrayPool<IResolverCommand>.Shared.Return(commands);
            }
        }

        interface IResolverCommand : ICloneable {
            void Reset(object from);
            bool Next(out object result);
        }

        sealed class SelfCommand : IResolverCommand {
            object current;

            public void Reset(object from) => current = from;

            public bool Next(out object result) {
                if (current != null) {
                    result = current;
                    current = null;
                    return true;
                }
                result = null;
                return false;
            }

            public object Clone() => new SelfCommand();
        }

        sealed class HierarchyParentCommand : IResolverCommand {
            readonly bool chained;
            Transform current;

            public HierarchyParentCommand(bool chained) {
                this.chained = chained;
            }

            public void Reset(object from) {
                if (from is Transform transform)
                    current = transform;
                else if (from is GameObject gameObject)
                    current = gameObject.transform;
                else if (from is Component component)
                    current = component.transform;
                else
                    current = null;
            }

            public bool Next(out object result) {
                if (current) {
                    result = current.parent;
                    current = chained ? result as Transform : null;
                    return true;
                }
                result = null;
                return false;
            }

            public object Clone() => new HierarchyParentCommand(chained);
        }

        sealed class HierarchyAnyChildCommand : IResolverCommand {
            readonly bool chained;
            readonly Queue<Transform> queue = new Queue<Transform>();

            public HierarchyAnyChildCommand(bool chained) {
                this.chained = chained;
            }

            public void Reset(object from) {
                queue.Clear();
                if (from is Transform transform)
                    EnqueueChildren(transform);
                else if (from is GameObject gameObject)
                    EnqueueChildren(gameObject.transform);
                else if (from is Component component)
                    EnqueueChildren(component.transform);
            }

            public bool Next(out object result) {
                if (queue.TryDequeue(out var current)) {
                    if (chained) EnqueueChildren(current);
                    result = current;
                    return true;
                }
                result = null;
                return false;
            }

            public object Clone() => new HierarchyAnyChildCommand(chained);

            void EnqueueChildren(Transform transform) {
                for (int i = 0, count = transform.childCount; i < count; i++)
                    queue.Enqueue(transform.GetChild(i));
            }
        }

        sealed class HierarchyRootCommand : IResolverCommand {
            readonly List<GameObject> roots = new List<GameObject>();
            int index;

            public void Reset(object from) {
                GameObject go;
                if (from is GameObject gameObject)
                    go = gameObject;
                else if (from is Component component)
                    go = component.gameObject;
                else
                    go = null;
                index = 0;
                if (!go) {
                    roots.Clear();
                    return;
                }
                go.scene.GetRootGameObjects(roots);
            }

            public bool Next(out object result) {
                if (index < roots.Count) {
                    result = roots[index++];
                    return true;
                }
                result = null;
                return false;
            }

            public object Clone() => new HierarchyRootCommand();
        }

        sealed class HierarchyChildCommand : IResolverCommand {
            readonly string childName;
            readonly Regex childNameRegex;
            Transform current;
            int index;

            public HierarchyChildCommand(string childName) {
                this.childName = childName;
                if (childName.Contains("*")) {
                    var splitted = childName.Split('*');
                    var sb = new StringBuilder();
                    bool asteriskAtStart = false, asteriskAtEnd = false;
                    for (int i = 1; i < splitted.Length; i++) {
                        if (string.IsNullOrEmpty(splitted[i])) {
                            if (sb.Length == 0)
                                asteriskAtStart = true;
                            else if (i >= splitted.Length - 1)
                                asteriskAtEnd = true;
                            continue;
                        }
                        if (sb.Length > 0) sb.Append(".*");
                        else if (!asteriskAtStart) sb.Append('^');
                        sb.Append(Regex.Escape(splitted[i]));
                    }
                    if (!asteriskAtEnd) sb.Append('$');
                    childNameRegex = new Regex(sb.ToString(), RegexOptions.Compiled);
                }
            }

            HierarchyChildCommand(string childName, Regex childNameRegex) {
                this.childName = childName;
                this.childNameRegex = childNameRegex;
            }

            public void Reset(object from) {
                if (from is Transform transform)
                    current = transform;
                else if (from is GameObject gameObject)
                    current = gameObject.transform;
                else if (from is Component component)
                    current = component.transform;
                else
                    current = null;
                index = 0;
            }

            public bool Next(out object result) {
                if (current)
                    for (int count = current.childCount; index < count; index++) {
                        var child = current.GetChild(index);
                        if (childNameRegex != null ? childNameRegex.IsMatch(child.name) : child.name == childName) {
                            result = child;
                            return true;
                        }
                    }
                result = null;
                return false;
            }

            public object Clone() => new HierarchyChildCommand(childName, childNameRegex);
        }

        sealed class SpecifyTypeCommand : IResolverCommand {
            static readonly List<Component> tempComponents = new List<Component>();
            readonly Queue<object> matches = new Queue<object>();
            readonly Type type;

            public SpecifyTypeCommand(Type type) {
                this.type = type;
            }

            public void Reset(object from) {
                matches.Clear();
                if (from == null) return;
                if (type.IsInstanceOfType(from))
                    matches.Enqueue(from);
                if (from is Component component) {
                    if (!component) return;
                    if (type == typeof(GameObject)) {
                        matches.Enqueue(component.gameObject);
                        return;
                    }
                    if (typeof(Component).IsAssignableFrom(type)) {
                        component.GetComponents(type, tempComponents);
                        foreach (var other in tempComponents)
                            if (other != component)
                                matches.Enqueue(other);
                        return;
                    }
                } else if (from is GameObject gameObject) {
                    if (!gameObject) return;
                    if (typeof(Component).IsAssignableFrom(type)) {
                        gameObject.GetComponents(type, tempComponents);
                        foreach (var other in tempComponents)
                            matches.Enqueue(other);
                        return;
                    }
                }
            }

            public bool Next(out object result) => matches.TryDequeue(out result);

            public object Clone() => new SpecifyTypeCommand(type);
        }

        sealed class PropertyCommand : IResolverCommand {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            readonly string propertyName;
            MemberInfo member;
            object current;

            public PropertyCommand(string propertyName) {
                this.propertyName = propertyName;
            }

            MemberInfo FetchMember() {
                if (current == null) return null;
                var type = current.GetType();
                var field = type.GetField(propertyName, flags);
                if (field != null) return field;
                var property = type.GetProperty(propertyName, flags);
                if (property != null) return property;
                return null;
            }

            public void Reset(object from) {
                current = from;
                if (current != null && (member == null || !member.DeclaringType.IsInstanceOfType(current)))
                    member = FetchMember();
            }

            public bool Next(out object result) {
                try {
                    if (current != null) {
                        if (current is UnityObject unityObject && !unityObject) {
                            result = null;
                            return false;
                        }
                        foreach (var resolver in customResolvers)
                            if (resolver(current, propertyName, member, out result))
                                return true;
                        if (member is FieldInfo field) {
                            result = field.GetValue(current);
                            return true;
                        }
                        if (member is PropertyInfo property) {
                            result = property.GetValue(current);
                            return true;
                        }
                    }
                    result = null;
                    return false;
                } finally {
                    current = null;
                }
            }

            public object Clone() => new PropertyCommand(propertyName);
        }

        enum ParsePathMode : byte {
            Undertermined,
            Hierarchy,
            Property,
        }
    }
}