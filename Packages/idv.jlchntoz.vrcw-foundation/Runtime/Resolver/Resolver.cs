using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
    public partial class Resolver : IEquatable<Resolver> {
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

        /// <summary>
        /// Create a resolver with the given path and destination type.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <param name="destinationType">The destination type (the final type to search through hierarchy) to resolve.</param>
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
            bool escaped = false;
            for (int i = 0, count = path.Length; i < count; i++) {
                if (escaped) {
                    sb.Append(path[i]);
                    escaped = false;
                    continue;
                }
                switch (path[i]) {
                    case '\\':
                        escaped = true;
                        break;
                    case '#':
                        if (sb.Length > 0) mode = ParseSegment(mode, sb, commandList, destinationType);
                        if (mode == ParsePathMode.Hierarchy) {
                            commandList.Add(new ChangeTypeCommand());
                            mode = ParsePathMode.Property;
                        } else
                            mode = ParsePathMode.Hierarchy;
                        break;
                    case '/':
                        switch (mode) {
                            case ParsePathMode.Undertermined:
                                if (sb.Length == 0) {
                                    commandList.Add(HierarchyRootCommand.instance);
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
            }
            if (sb.Length > 0) {
                if (mode == ParsePathMode.Undertermined) {
                    if (commandList.Count == 0)
                        commandList.Add(new ChangeTypeCommand(destinationType));
                    mode = ParsePathMode.Property;
                }
                ParseSegment(mode, sb, commandList);
            }
            if (mode == ParsePathMode.Hierarchy)
                commandList.Add(new ChangeTypeCommand(destinationType));
            else
                for (int i = commandList.Count - 1; i > 0; i--)
                    if (commandList[i] is ChangeTypeCommand cType) {
                        if (cType.type != destinationType)
                            commandList[i] = new ChangeTypeCommand(destinationType);
                        break;
                    }
            commands = commandList.ToArray();
            cache[(path, destinationType)] = commands;
        }

        static ParsePathMode ParseSegment(ParsePathMode currentMode, StringBuilder sb, IList<IResolverCommand> commandList, Type defaultType = null) {
            if (sb.Length == 0) {
                commandList.Add(SelfCommand.instance);
                return currentMode;
            }
            var pathSeg = sb.ToString();
            sb.Clear();
            switch (pathSeg) {
                case ".":
                case "this":
                    if (currentMode != ParsePathMode.Hierarchy) {
                        if (commandList.Count == 0)
                            commandList.Add(new ChangeTypeCommand(defaultType));
                        else
                            commandList.Add(SelfCommand.instance);
                        return ParsePathMode.Property;
                    }
                    goto default;
                case "..":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(HierarchyParentCommand.nonChainedInstance);
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "..*":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(HierarchyParentCommand.chainedInstance);
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "*":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(HierarchyAnyChildCommand.nonChainedInstance);
                        return ParsePathMode.Hierarchy;
                    }
                    goto default;
                case "**":
                    if (currentMode != ParsePathMode.Property) {
                        commandList.Add(HierarchyAnyChildCommand.chainedInstance);
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
                            if (commandList.Count == 0) commandList.Add(new ChangeTypeCommand(defaultType));
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

        enum ParsePathMode : byte {
            Undertermined,
            Hierarchy,
            Property,
        }
    }
}