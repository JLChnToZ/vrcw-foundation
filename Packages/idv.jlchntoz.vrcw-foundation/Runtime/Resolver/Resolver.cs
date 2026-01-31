using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    /// <summary>
    /// A generic resolver that supports both property and hierarchy resolution.
    /// </summary>
    public partial class Resolver {
        static readonly List<TryResolveMemberDelegate> customResolvers = new List<TryResolveMemberDelegate>();
        readonly List<IResolverCommand> commandList = new List<IResolverCommand>();
        IResolverCommand[] commands;

        /// <summary>
        /// Register a custom resolve provider.
        /// </summary>
        public static event TryResolveMemberDelegate CustomResolveProvider {
            add => customResolvers.Add(value);
            remove => customResolvers.Remove(value);
        }

        /// <summary>
        /// Create a new resolver.
        /// </summary>
        /// <returns>A new resolver.</returns>
        public static Resolver Create() => new Resolver();

        Resolver() { }

        /// <summary>
        /// Configurate this resolver to resolve the path from the current object.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <param name="mode">The mode to parse the path. Default is <see cref="ParsePathMode.AutoDetect"/>.</param>
        /// <returns>Current resolver for chaining.</returns>
        /// <remarks>
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
        public Resolver WithPath(string path, ParsePathMode mode = ParsePathMode.AutoDetect) {
            commands = null;
            if (string.IsNullOrWhiteSpace(path)) return this;
            var sb = new StringBuilder(path.Length);
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
                        if (sb.Length > 0) mode = ParseSegment(mode, sb);
                        if (mode == ParsePathMode.Hierarchy) {
                            commandList.Add(new ChangeTypeCommand());
                            mode = ParsePathMode.Property;
                        } else
                            mode = ParsePathMode.Hierarchy;
                        break;
                    case '/':
                        switch (mode) {
                            case ParsePathMode.AutoDetect:
                                mode = ParsePathMode.Hierarchy;
                                if (sb.Length == 0) {
                                    commandList.Add(HierarchyRootCommand.instance);
                                    break;
                                }
                                goto case ParsePathMode.Hierarchy;
                            case ParsePathMode.Hierarchy:
                                mode = ParseSegment(mode, sb);
                                break;
                        }
                        break;
                    case '*':
                        if (mode == ParsePathMode.AutoDetect) mode = ParsePathMode.Hierarchy;
                        goto default;
                    default:
                        sb.Append(path[i]);
                        break;
                }
            }
            if (sb.Length > 0) ParseSegment(mode, sb);
            return this;
        }

        ParsePathMode ParseSegment(ParsePathMode currentMode, StringBuilder sb) {
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
                        case ParsePathMode.AutoDetect:
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
        /// Configurate this resolver to resolve the parent of the current object.
        /// </summary>
        /// <param name="grandParent">Whether to resolve the grand parent.</param>
        /// <returns>Current resolver for chaining.</returns>
        public Resolver WithParent(bool grandParent = false) {
            commands = null;
            commandList.Add(grandParent ? HierarchyParentCommand.chainedInstance : HierarchyParentCommand.nonChainedInstance);
            return this;
        }

        /// <summary>
        /// Configurate this resolver to resolve the child of the current object.
        /// </summary>
        /// <param name="name">The name of the child to resolve.</param>
        /// <returns>Current resolver for chaining.</returns>
        public Resolver WithChild(string name) {
            commands = null;
            commandList.Add(string.IsNullOrEmpty(name) ? HierarchyAnyChildCommand.nonChainedInstance as IResolverCommand : new HierarchyChildCommand(name));
            return this;
        }

        /// <summary>
        /// Configurate this resolver to resolve any child of the current object.
        /// </summary>
        /// <param name="grandChild">Whether to resolve the grand child.</param>
        /// <returns>Current resolver for chaining.</returns>
        public Resolver WithChild(bool grandChild = false) {
            commands = null;
            commandList.Add(grandChild ? HierarchyAnyChildCommand.chainedInstance : HierarchyAnyChildCommand.nonChainedInstance);
            return this;
        }

        /// <summary>
        /// Configurate this resolver to resolve the property of the current object.
        /// </summary>
        /// <param name="name">The name of the property to resolve.</param>
        /// <returns>Current resolver for chaining.</returns>
        public Resolver WithProperty(string name) {
            commands = null;
            commandList.Add(new PropertyCommand(name));
            return this;
        }

        /// <summary>
        /// Configurate this resolver to resolve the type of the current object.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>Current resolver for chaining.</returns>
        /// <remarks>
        /// If current object is a <see cref="UnityEngine.Component"/> or <see cref="UnityEngine.GameObject"/>,
        /// this will try to get the attached object with matching type, or all attached objects if no type specified;
        /// otherwise, it will try to cast the object to the specified type.
        /// </remarks>
        public Resolver WithType(Type type = null) {
            commands = null;
            commandList.Add(new ChangeTypeCommand(type));
            return this;
        }

        /// <inheritdoc cref="WithType(Type)"/>
        /// <typeparam name="T">The type to resolve.</typeparam>
        public Resolver WithType<T>() => WithType(typeof(T));

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
        /// The mode of the path parsing.
        /// </summary>
        public enum ParsePathMode : byte {
            /// <summary>
            /// Automatically detect the mode.
            /// If the path contains path-like separator at the first segment, it will treat as hierarchy;
            /// otherwise, it will treat as property.
            /// </summary>
            AutoDetect,
            /// <summary>
            /// Resolve as hierarchy.
            /// </summary>
            Hierarchy,
            /// <summary>
            /// Resolve as property.
            /// </summary>
            Property,
        }
    }
}